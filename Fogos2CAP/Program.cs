using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Fogos2CAP
{
    class Program
    {
        static void Main(string[] args)
        {
            WebRequest wr = WebRequest.Create("https://fogos.pt/new/fires");
            wr.Method = "GET";
            wr.Proxy = GetProxy();
            WebResponse response = wr.GetResponse();
            var sr = new StreamReader(response.GetResponseStream());
            var myStr = sr.ReadToEnd();
            JObject jo = JObject.Parse(myStr);
            string xml = string.Empty;
            if (jo.Value<bool>("success"))
            {
                JArray data = jo.Value<JArray>("data");
                alert oAlert = new alert();
                oAlert.identifier = Guid.NewGuid().ToString();
                oAlert.msgType = alertMsgType.Alert;
                oAlert.scope = alertScope.Public;
                oAlert.sender = "https://fogos.pt";
                oAlert.sent = DateTime.UtcNow;
                oAlert.status = alertStatus.Actual;
                List<alertInfo> infoList = new List<alertInfo>();
                foreach (var item in data)
                {


                    alertInfo info = new alertInfo();
                    info.category = new[] { alertInfoCategory.Fire };
                    info.certainty = alertInfoCertainty.Observed;
                    info.description = item.Value<string>("location");
                    info.language = "pt-PT";
                    info.description = string.Format("Status:{0}-Homens:{1}-M.Aereos:{2}", item.Value<string>("status"), item.Value<string>("man"), item.Value<string>("aerial"));
                    if (item.Value<bool>("important"))
                    {
                        info.description += item.Value<string>("extra");
                    }
                    int raio = 0;
                    switch (item.Value<string>("statusCode"))
                    {
                        case "10":
                        case "9":
                            info.severity = alertInfoSeverity.Minor;
                            break;
                        case "8":
                        case "7":
                            info.severity = alertInfoSeverity.Moderate;
                            raio = 1;
                            break;
                        case "5":
                            info.severity = alertInfoSeverity.Severe;
                            raio = 3;
                            if (item.Value<bool>("important"))
                            {
                                info.severity = alertInfoSeverity.Extreme;
                                raio = 5;
                            }
                            break;
                    }
                    info.@event = "Ocorrencia";
                    info.urgency = alertInfoUrgency.Immediate;
                    info.expires = DateTime.Now.AddDays(1);
                    info.web = "https://fogos.pt";
                    List<alertInfoArea> areaList = new List<alertInfoArea>();
                    alertInfoArea area = new alertInfoArea();
                    area.circle = new[] { string.Format("{0},{1} {2}", item.Value<string>("lat"), item.Value<string>("lng"), raio) };
                    area.areaDesc = info.description;
                    areaList.Add(area);
                    info.area = areaList.ToArray();

                    infoList.Add(info);

                    //  break;
                }
                oAlert.info = infoList.ToArray();
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(alert));

                using (StringWriter xw = new Utf8StringWriter())
                {
                    xmlSerializer.Serialize(xw, oAlert);
                    xml = xw.ToString();
                }


            }

            Console.Write(xml);
            Console.ReadKey();

        }

        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        private static IWebProxy GetProxy()
        {
            var proxyURL = ConfigurationManager.AppSettings["proxyURL"];
            WebProxy wp = new WebProxy(proxyURL);
            wp.Credentials = new NetworkCredential("jz61ee", "delphi0917", "europe");
            return wp;
        }
    }
}
