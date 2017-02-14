using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace XData.Client.Components
{
    public class ApiClient
    {
        protected HttpClient HttpClient = new HttpClient();

        public ApiClient(string baseAddress)
        {
            HttpClient.BaseAddress = new Uri(baseAddress);
        }

        public ApiClient(Uri baseAddress)
        {
            HttpClient.BaseAddress = baseAddress;

        }

        public XElement Get(string relativeUri)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            var response = HttpClient.GetAsync(relativeUri).Result;
            string text = response.Content.ReadAsStringAsync().Result;        
            XElement result = XElement.Parse(text);
            return ReturnResult(response, result);
        }

        public async Task<XElement> GetAsync(string relativeUri)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            var response = await HttpClient.GetAsync(relativeUri);
            string text = await response.Content.ReadAsStringAsync();
            XElement result = XElement.Parse(text);
            return ReturnResult(response, result);
        }

        public XElement Put(string relativeUri, XElement value)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            HttpContent content = new StringContent(value.ToString(), Encoding.UTF8, "application/xml");
            var response = HttpClient.PutAsync(relativeUri, content).Result;
            string text = response.Content.ReadAsStringAsync().Result;
            XElement result = XElement.Parse(text);
            return ReturnResult(response, result);
        }

        public async Task<XElement> PutAsync(string relativeUri, XElement value)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            HttpContent content = new StringContent(value.ToString(), Encoding.UTF8, "application/xml");
            var response = await HttpClient.PutAsync(relativeUri, content);
            string text = await response.Content.ReadAsStringAsync();
            XElement result = XElement.Parse(text);
            return ReturnResult(response, result);
        }

        public XElement Post(string relativeUri, XElement value)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            HttpContent content = new StringContent(value.ToString(), Encoding.UTF8, "application/xml");
            var response = HttpClient.PostAsync(relativeUri, content).Result;
            string text = response.Content.ReadAsStringAsync().Result;
            XElement result = XElement.Parse(text);
            return ReturnResult(response, result);
        }

        public async Task<XElement> PostAsync(string relativeUri, XElement value)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            HttpContent content = new StringContent(value.ToString(), Encoding.UTF8, "application/xml");
            var response = await HttpClient.PostAsync(relativeUri, content);
            string text = await response.Content.ReadAsStringAsync();
            XElement result = XElement.Parse(text);
            return ReturnResult(response, result);
        }

        public XElement Delete(string relativeUri)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            var response = HttpClient.DeleteAsync(relativeUri).Result;
            string text = response.Content.ReadAsStringAsync().Result;
            XElement result = XElement.Parse(text);
            return ReturnResult(response, result);
        }

        public async Task<XElement> DeleteAsync(string relativeUri)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            var response = await HttpClient.DeleteAsync(relativeUri);
            string text = await response.Content.ReadAsStringAsync();
            XElement result = XElement.Parse(text);
            return ReturnResult(response, result);
        }

        public XElement Delete(string relativeUri, XElement value)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            HttpContent content = new StringContent(value.ToString(), Encoding.UTF8, "application/xml");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, relativeUri);
            request.Content = content;

            var response = HttpClient.SendAsync(request).Result;
            string text = response.Content.ReadAsStringAsync().Result;
            XElement result = XElement.Parse(text);
            return ReturnResult(response, result);
        }

        public async Task<XElement> DeleteAsync(string relativeUri, XElement value)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            HttpContent content = new StringContent(value.ToString(), Encoding.UTF8, "application/xml");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, relativeUri);
            request.Content = content;

            var response = await HttpClient.SendAsync(request);
            string text = await response.Content.ReadAsStringAsync();
            XElement result = XElement.Parse(text);
            return ReturnResult(response, result);
        }

        public string Login(string relativeUri, string userName, string password, bool rememberMe)
        {
            XElement apiVerificationToken = GetApiVerificationTokenValue(relativeUri);

            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            XElement value = new XElement("Login");
            value.SetElementValue("UserName", userName);
            value.SetElementValue("Password", password);
            value.SetElementValue("RememberMe", rememberMe.ToString());
            value.Add(apiVerificationToken);

            HttpContent content = new StringContent(value.ToString(), Encoding.UTF8, "application/xml");
            var response = HttpClient.PostAsync(relativeUri, content).Result;
            response.EnsureSuccessStatusCode();

            string text = response.Content.ReadAsStringAsync().Result;
            XElement result = XElement.Parse(text);
            return result.Value;
        }

        public void Logout(string relativeUri)
        {
            XElement apiVerificationToken = GetApiVerificationTokenValue(relativeUri);

            XElement value = new XElement("Logout");
            value.Add(apiVerificationToken);

            HttpContent content = new StringContent(value.ToString(), Encoding.UTF8, "application/xml");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, relativeUri);
            request.Content = content;

            HttpResponseMessage response = HttpClient.SendAsync(request).Result;
        }

        protected XElement GetApiVerificationTokenValue(string relativeUri)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");
            return XElement.Parse(HttpClient.GetStringAsync(relativeUri).Result);
        }

        public string ChangePassword(string relativeUri, string oldPassword, string newPassword, string confirmPassword)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/xml");

            XElement value = new XElement("ChangePassword");
            value.SetElementValue("OldPassword", oldPassword);
            value.SetElementValue("NewPassword", newPassword);
            value.SetElementValue("ConfirmPassword", confirmPassword);

            HttpContent content = new StringContent(value.ToString(), Encoding.UTF8, "application/xml");
            var response = HttpClient.PutAsync(relativeUri, content).Result;

            string text = response.Content.ReadAsStringAsync().Result;
            XElement result = XElement.Parse(text);
            result = ReturnResult(response, result);
            return result.Value;        
        }

        protected XElement ReturnResult(HttpResponseMessage response, XElement result)
        {
            if (response.IsSuccessStatusCode)
            {
                if (result.Name.LocalName == "Error")
                {
                    throw new HttpResponseException(response.StatusCode, result);
                }
                return result;
            }
            else
            {
                throw new HttpResponseException(response.StatusCode, result);
            }
        }

        protected const string Request_Verification_Token_Name = "__RequestVerificationToken";

        public bool FormLogin(string relativeUri, string userName, string password, bool rememberMe)
        {
            string token = GetRequestVerificationTokenValue(relativeUri);

            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "text/html");

            Dictionary<string, string> nameValues = new Dictionary<string, string>();
            nameValues.Add(Request_Verification_Token_Name, token);
            nameValues.Add("UserName", userName);
            nameValues.Add("Password", password);
            nameValues.Add("RememberMe", rememberMe.ToString());
            FormUrlEncodedContent content = new FormUrlEncodedContent(nameValues);

            HttpResponseMessage response = HttpClient.PostAsync(relativeUri, content).Result;
            response.EnsureSuccessStatusCode();

            string html = response.Content.ReadAsStringAsync().Result;
            // <form action="/Account/Logout"
            string pattern = @"<\s*form\s*action\s*=\s*" + "\"" + "/Account/Logout" + "\"";
            string s = Regex.Match(html, pattern).Value;
            return !string.IsNullOrWhiteSpace(s);
        }

        public void FormLogout(string relativeUri)
        {
            string token = GetRequestVerificationTokenValue(string.Empty);
            Dictionary<string, string> nameValues = new Dictionary<string, string>();
            nameValues.Add(Request_Verification_Token_Name, token);
            FormUrlEncodedContent content = new FormUrlEncodedContent(nameValues);
            HttpResponseMessage response = HttpClient.PostAsync(relativeUri, content).Result;
        }

        protected string GetRequestVerificationTokenValue(string relativeUri)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "text/html");
            string html = HttpClient.GetStringAsync(relativeUri).Result;

            // name="__RequestVerificationToken" contentType="hidden" value="VuT-QxOhABS6H1K24Bl15iH40QeHY6gTgP0qIu_avXSJ4XMD_h2x05-r2qfJYiAx3szYgMKQkhE91yveHr_PPsj58CxprT2y_2l3EVmA-sI1"
            string pattern = @"name\s*=\s*" + "\"" + Request_Verification_Token_Name + "\"" + @".+value\s*=\s*"".+?""";
            string value = Regex.Match(html, pattern).Value;
            int index = value.IndexOf(Request_Verification_Token_Name);
            value = value.Substring(index + Request_Verification_Token_Name.Length + 1);
            index = value.IndexOf('"');
            value = value.Substring(index);
            value = value.TrimStart('"').TrimEnd('"');
            return value;
        }

        public bool FormChangePassword(string relativeUri, string oldPassword, string newPassword, string confirmPassword)
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("Accept", "text/html");

            Dictionary<string, string> nameValues = new Dictionary<string, string>();
            nameValues.Add("OldPassword", oldPassword);
            nameValues.Add("NewPassword", newPassword);
            nameValues.Add("ConfirmPassword", confirmPassword);
            FormUrlEncodedContent content = new FormUrlEncodedContent(nameValues);

            HttpResponseMessage response = HttpClient.PostAsync(relativeUri, content).Result;
            response.EnsureSuccessStatusCode();

            string html = response.Content.ReadAsStringAsync().Result;
            return html.Contains("Change password successful");
        }


    }
}
