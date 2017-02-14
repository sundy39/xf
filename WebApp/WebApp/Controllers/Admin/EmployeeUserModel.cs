using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Xml.Linq;
using XData.Data.Services;
using XData.WebApp.Models;

namespace XData.Web.Mvc.Admin.Models
{
    public class EmployeeUserModel
    {
        protected UsersService UsersService = new UsersService();

        public HttpResponseMessage Create(object value, ApiController apiController)
        {
            JObject jObj = value as JObject;
            JToken jToken = jObj.SelectToken("UserName");
            string userName = ((JValue)jToken).Value.ToString();
            jToken = jObj.SelectToken("IsDisabled");
            bool isDisabled = (bool)(((JValue)jToken).Value);
            jToken = jObj.Last.First.SelectToken("Id");
            int employeeId = (int)Convert.ChangeType(((JValue)jToken).Value, TypeCode.Int32);

            //
            CreateUserModel model = new CreateUserModel() { EmployeeId = employeeId, UserName = userName, IsDisabled = isDisabled };
            apiController.Validate(model);
            ModelStateDictionary modelState = apiController.ModelState;
            if (!modelState.IsValid)
            {
                XElement element = new XElement("User");
                element.SetElementValue("EmployeeId", employeeId);
                element.SetElementValue("UserName", userName);
                element.SetElementValue("IsDisabled", isDisabled.ToString());

                throw modelState.GetElementValidationException(element);
            }

            UsersService.Create(employeeId, userName, isDisabled);

            XElement user = UsersService.GetUser(userName);
            var obj = new { Id = int.Parse(user.Element("Id").Value), EmployeeId = employeeId, UserName = userName, IsDisabled = isDisabled };
            string content = Newtonsoft.Json.JsonConvert.SerializeObject(obj);

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            return response;
        }

        public HttpResponseMessage Update(object value, ApiController apiController)
        {
            JObject jObj = value as JObject;
            JToken jToken = jObj.SelectToken("Id");
            int id = (int)Convert.ChangeType(((JValue)jToken).Value, TypeCode.Int32);
            jToken = jObj.SelectToken("UserName");
            string userName = ((JValue)jToken).Value.ToString();
            jToken = jObj.SelectToken("IsDisabled");
            bool isDisabled = (bool)(((JValue)jToken).Value);

            XElement element = new XElement("User");
            element.SetElementValue("Id", id);
            element.SetElementValue("UserName", userName);
            element.SetElementValue("IsDisabled", isDisabled.ToString());

            SetUserNameModel model = new SetUserNameModel() { NewUserName = userName };
            apiController.Validate(model);
            ModelStateDictionary modelState = apiController.ModelState;
            if (!modelState.IsValid) throw modelState.GetElementValidationException(element);

            UsersService.Update(element);

            HttpResponseMessage response = new HttpResponseMessage();
            response.Content = new StringContent(value.ToString(), System.Text.Encoding.UTF8, "application/json");
            return response;
        }


    }
}