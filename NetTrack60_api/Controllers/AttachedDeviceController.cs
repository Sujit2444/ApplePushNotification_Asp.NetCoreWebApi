using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NetTrack60_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttachedDeviceController : ControllerBase
    {
        private readonly IConfiguration _config;
       
        // GET: api/<AttachedDeviceController>
        public AttachedDeviceController(IConfiguration configuration)
        {
            Utils.LogToFile("[INFO]", "Calling AttachedDeviceController(IConfiguration configuration)");
            _config = configuration;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

      
        [HttpGet("{DeviceToken}/{apnsId}/{type}")]
        public string SendIosAlert(string DeviceToken, string apnsId, string type)
        {
            Utils.LogToFile("[INFO]","Calling SendIosAlert()");
            ResponseData responseData = new ResponseData();
            string message =_config["appsettings:IdentifyMessage"];
            var url = string.Format(_config["appsettings:AppleBaseUrl"]);
            Utils.LogToFile("[INFO]", $"Apple Base Url:{url}");
            string certificatePath = _config["appsettings:CertificatePath"];
            Utils.LogToFile("[INFO]", $"Apple Certification Path:{certificatePath}");
            string certificatePassword = _config["appsettings:CertificatePassword"];
            Utils.LogToFile("[INFO]", $"Apple Certifcation Password:{certificatePassword}");
            string topic = _config["appsettings:AppleTopic"];
            Utils.LogToFile("[INFO]", $"Apple Topic:{topic}");
            Utils.LogToFile("[INFO]", $"Device Token:{DeviceToken}");
            Utils.LogToFile("[INFO]", $"APNS ID(Device Id):{apnsId}");
            Utils.LogToFile("[INFO]", $"Alert Type:{type}");

            try
            {
                var certData = System.IO.File.ReadAllBytes(certificatePath);
                var certificate = new X509Certificate2(certData, certificatePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                using (var httpHandler = new HttpClientHandler { SslProtocols = System.Security.Authentication.SslProtocols.Tls12 })
                {
                    var path = $"/3/device/{DeviceToken}";
                    httpHandler.ClientCertificates.Add(certificate);
                    using (var httpClient = new HttpClient(httpHandler, true))
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url + path)))
                        {
                            Utils.LogToFile("[INFO]", $"Apple Request Path:{new Uri(url + path)}");
                            SetHttpRequestMessage(request,path, apnsId, topic, type, message);                       
                            try
                            {
                                using (HttpResponseMessage httpResponseMessage = httpClient.SendAsync(request).GetAwaiter().GetResult())
                                {
                                    Utils.LogToFile("[INFO]", $"Send Http Request To The Apple Server");
                                    var responseContent = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                                    if (responseContent.Equals(""))
                                    {
                                        responseContent = "{\"reason\":\"\"";                 
                                    }
                                    int statusCode = (int)httpResponseMessage.StatusCode;
                                    responseContent = responseContent.Replace('}', ' ');
                                    responseContent += $",\"StatusCode\":\"{statusCode.ToString()}\"";                           
                                    Utils.LogToFile("[INFO]", $"Status Code:{statusCode}");
                                    string ReasonPhrase = httpResponseMessage.ReasonPhrase;
                                    responseContent += $",\"ReasonPhrase\":\"{ReasonPhrase}\"{"}"}";
                                    responseData = JsonConvert.DeserializeObject<ResponseData>(responseContent);
                                    Utils.LogToFile("[INFO]", $"Response Content:{responseData.Reason}");
                                    Utils.LogToFile("[INFO]", $"Status Code Reason Phrase:{ReasonPhrase}");
                                }
                            }
                            catch (Exception ex)
                            {
                                responseData.Reason = ex.Message;
                                responseData.ReasonPhrase = "Exception";
                                Utils.LogToFile("[EXCEPTION]", $"Exception SendIosAlert(): {ex.Message} ");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                responseData.Reason = ex.Message;
                responseData.ReasonPhrase = "Exception";
                Utils.LogToFile("[EXCEPTION]", $"Exception SendIosAlert(): {ex.Message}");
            }

            return JsonConvert.SerializeObject(responseData);     
        }

        private void SetHttpRequestMessage(HttpRequestMessage request,string path,string apnsId,string topic,string type,string message)
        {
            Utils.LogToFile("[INFO]", "Calling SetHttpRequestMessage()");
            try
            {
                request.Headers.TryAddWithoutValidation(":method", "POST");
                request.Headers.TryAddWithoutValidation(":scheme", "https");
                request.Headers.TryAddWithoutValidation(":path", path);
                request.Headers.Host = "api.push.apple.com";
                request.Headers.Add("apns-id", apnsId);
                request.Headers.Add("apns-push-type", "alert");
                request.Headers.Add("apns-topic", topic);
                request.Headers.Add("apns-priority", "10");
                request.Headers.Add("apns-expiration", "0");
                JObject payload;
                if (type == "identify")
                {
                    payload = new JObject {
                                 {
                                   "aps", new JObject
                                    {
                                      { "alert", new JObject{

                                            { "body", message },
                                        }
                                       },
                                       { "sound", "default" },
                                       { "badge" , 1},
                                       { "content-available", 1 },
                                       { "category" , type}
                                    }
                                  }
                              };
                }
                else
                {
                    payload = new JObject {
                                 {
                                   "aps", new JObject
                                    {
                                      { "badge" , 1},
                                      { "content-available", 1 },
                                      { "category" , type}
                                    }
                                  },
                              };
                }


                request.Content = new StringContent(payload.ToString());
                request.Version = new Version(2, 0);
            }
            catch (Exception ex)
            {
                Utils.LogToFile("[EXCEPTION]", $"Exception SetHttpRequestMessage(): {ex.Message}");
            }
        }
    }
}
