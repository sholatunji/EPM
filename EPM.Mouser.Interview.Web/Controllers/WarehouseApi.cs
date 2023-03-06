using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml.Linq;
using EPM.Mouser.Interview.Data;
using EPM.Mouser.Interview.Models;
using Microsoft.AspNetCore.Mvc;

namespace EPM.Mouser.Interview.Web.Controllers
{
    public class WarehouseApi : Controller
    {
        /*
         *  Action: GET
         *  Url: api/warehouse/id
         *  This action should return a single product for an Id
         */

        [HttpGet]
        [Route("api/warehouse/{id?}")]
        public async Task<JsonResult> GetProduct(long id)
        {
            var wr = new Data.WarehouseRepository();
            var product = await wr.Get(id);
            return Json(product);
        }

        /*
         *  Action: GET
         *  Url: api/warehouse
         *  This action should return a collection of products in stock
         *  In stock means In Stock Quantity is greater than zero and In Stock Quantity is greater than the Reserved Quantity
         */
        [HttpGet]
        [Route("api/warehouse")]
        public async Task<JsonResult> GetPublicInStockProductsAsync()
        {
            var wr = new Data.WarehouseRepository();
            var products = await wr.List();
            var p = products.ToList().Where(x => x.InStockQuantity > 0 && x.InStockQuantity > x.ReservedQuantity);
            if (p == null)
                return Json(null);
            return Json(p);
        }


        /*
         *  Action: GET
         *  Url: api/warehouse/order
         *  This action should return a EPM.Mouser.Interview.Models.UpdateResponse
         *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.UpdateQuantityRequest in JSON format in the body of the request
         *       {
         *           "id": 1,
         *           "quantity": 1
         *       }
         *
         *  This action should increase the Reserved Quantity for the product requested by the amount requested
         *
         *  This action should return failure (success = false) when:
         *     - ErrorReason.NotEnoughQuantity when: The quantity being requested would increase the Reserved Quantity to be greater than the In Stock Quantity.
         *     - ErrorReason.QuantityInvalid when: A negative number was requested
         *     - ErrorReason.InvalidRequest when: A product for the id does not exist
        */
        [HttpGet]
        [Route("api/warehouse/order")]
        public async Task<JsonResult> OrderItem()
        {
            UpdateResponse ur = new UpdateResponse();
            ur.Success = true;
            using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
            {
                var body = stream.ReadToEndAsync();
                UpdateQuantityRequest? uqr = new UpdateQuantityRequest();
                var properties = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
                uqr = JsonSerializer.Deserialize<UpdateQuantityRequest>(body.Result, properties);

                if (uqr != null)
                {
                    
                    if (uqr.Quantity < 0)
                    {
                        ur.ErrorReason = ErrorReason.QuantityInvalid;
                        ur.Success = false;
                        return Json(ur);
                    }

                    var wr = new Data.WarehouseRepository();
                    var products = await wr.List();
                    var p = products.ToList().Where(x => x.Id == uqr.Id).First();
                    if (p == null)
                    {
                        ur.ErrorReason = ErrorReason.InvalidRequest;
                        ur.Success = false;
                    }

                    p.ReservedQuantity += uqr.Quantity;

                    if (p.ReservedQuantity > p.InStockQuantity)
                    {
                        ur.ErrorReason = ErrorReason.NotEnoughQuantity;
                        ur.Success = false;
                    }
                } 
            }
            return Json(ur);
        }

        /*
         *  Url: api/warehouse/ship
         *  This action should return a EPM.Mouser.Interview.Models.UpdateResponse
         *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.UpdateQuantityRequest in JSON format in the body of the request
         *       {
         *           "id": 1,
         *           "quantity": 1
         *       }
         *
         *
         *  This action should:
         *     - decrease the Reserved Quantity for the product requested by the amount requested to a minimum of zero.
         *     - decrease the In Stock Quantity for the product requested by the amount requested
         *
         *  This action should return failure (success = false) when:
         *     - ErrorReason.NotEnoughQuantity when: The quantity being requested would cause the In Stock Quantity to go below zero.
         *     - ErrorReason.QuantityInvalid when: A negative number was requested
         *     - ErrorReason.InvalidRequest when: A product for the id does not exist
        */
        [Route("api/warehouse/ship")]
        public async Task<JsonResult> ShipItemAsync()
        {
            UpdateResponse ur = new UpdateResponse();
            ur.Success = true;

            using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
            {
                var body = stream.ReadToEndAsync();
                UpdateQuantityRequest? uqr = new UpdateQuantityRequest();
                var properties = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
                uqr = JsonSerializer.Deserialize<UpdateQuantityRequest>(body.Result, properties);

                if (uqr.Quantity < 0)
                {
                    ur.ErrorReason = ErrorReason.QuantityInvalid;
                    ur.Success = false;
                    return Json(ur);
                }

                var wr = new Data.WarehouseRepository();
                var products = await wr.List();
                var p = products.ToList().Where(x => x.Id == uqr.Id).First();
                if (p == null)
                {
                    ur.ErrorReason = ErrorReason.InvalidRequest;
                    ur.Success = false;
                }

                int resQty = p.ReservedQuantity - uqr.Quantity;
                p.ReservedQuantity = ((resQty) < 0) ? 0 : resQty;
                p.InStockQuantity -= uqr.Quantity;
                if (p.InStockQuantity < 0)
                {
                    ur.ErrorReason = ErrorReason.NotEnoughQuantity;
                    ur.Success = false;
                }
            }
            return Json(ur);
        }

        /*
        *  Url: api/warehouse/restock
        *  This action should return a EPM.Mouser.Interview.Models.UpdateResponse
        *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.UpdateQuantityRequest in JSON format in the body of the request
        *       {
        *           "id": 1,
        *           "quantity": 1
        *       }
        *
        *
        *  This action should:
        *     - increase the In Stock Quantity for the product requested by the amount requested
        *
        *  This action should return failure (success = false) when:
        *     - ErrorReason.QuantityInvalid when: A negative number was requested
        *     - ErrorReason.InvalidRequest when: A product for the id does not exist
        */
        [Route("api/warehouse/restock")]
        public async Task<JsonResult> RestockItemAsync()
        {
            UpdateResponse ur = new UpdateResponse();
            ur.Success = true;
            using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
            {
                var body = stream.ReadToEndAsync();
                UpdateQuantityRequest? uqr = new UpdateQuantityRequest();
                var properties = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
                uqr = JsonSerializer.Deserialize<UpdateQuantityRequest>(body.Result, properties);

                if (uqr.Quantity < 0)
                {
                    ur.ErrorReason = ErrorReason.QuantityInvalid;
                    ur.Success = false;
                    return Json(ur);
                }

                var wr = new Data.WarehouseRepository();
                var products = await wr.List();
                var p = products.ToList().Where(x => x.Id == uqr.Id).First();
                if (p == null)
                {
                    ur.ErrorReason = ErrorReason.InvalidRequest;
                    ur.Success = false;
                }
                p.InStockQuantity += uqr.Quantity;

            }

            return Json(ur);
        }

        /*
        *  Url: api/warehouse/add
        *  This action should return a EPM.Mouser.Interview.Models.CreateResponse<EPM.Mouser.Interview.Models.Product>
        *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.Product in JSON format in the body of the request
        *       {
        *           "id": 1,
        *           "inStockQuantity": 1,
        *           "reservedQuantity": 1,
        *           "name": "product name"
        *       }
        *
        *
        *  This action should:
        *     - create a new product with:
        *          - The requested name - But forced to be unique - see below
        *          - The requested In Stock Quantity
        *          - The Reserved Quantity should be zero
        *
        *       UNIQUE Name requirements
        *          - No two products can have the same name
        *          - Names should have no leading or trailing whitespace before checking for uniqueness
        *          - If a new name is not unique then append "(x)" to the name [like windows file system does, where x is the next avaiable number]
        *
        *
        *  This action should return failure (success = false) and an empty Model property when:
        *     - ErrorReason.QuantityInvalid when: A negative number was requested for the In Stock Quantity
        *     - ErrorReason.InvalidRequest when: A blank or empty name is requested
        */
        [Route("api/warehouse/add")]
        public async Task<JsonResult> AddNewProductAsync()
        {

            using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
            {
                CreateResponse<Product> product = new CreateResponse<Product>();

                var body = stream.ReadToEndAsync();
                Product? prod = new Product();
                var properties = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
                prod = JsonSerializer.Deserialize<Product>(body.Result, properties);

                if (prod != null)
                {
                    string p_name = prod.Name.Trim();
                    if (string.IsNullOrEmpty(p_name))
                    {
                        product.ErrorReason = ErrorReason.InvalidRequest;
                        product.Success = false;
                        return Json(product);
                    }

                    if (prod.ReservedQuantity < 0)
                    {
                        product.ErrorReason = ErrorReason.QuantityInvalid;
                        product.Success = false;
                        return Json(product);
                    }

                    //get if product name already exist
                    var wr = new Data.WarehouseRepository();
                    var products = await wr.List();
                    var p = products.ToList().Where(x => x.Name.Contains(p_name));

                    p_name = string.Format("{0}", p_name);
                    if (p.Count() > 0)
                        p_name = string.Format("{0} ({1})", p_name, p.Count().ToString());

                    Product  pp = new Product();
                    pp.Name = p_name;
                    pp.Id = prod.Id;
                    pp.InStockQuantity = prod.InStockQuantity;
                    pp.ReservedQuantity = 0;

                    product.Model = pp;
                }
                return Json(product);
            }
        }
    }
}
