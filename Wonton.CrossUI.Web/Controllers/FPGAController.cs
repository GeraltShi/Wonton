using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Wonton.Common;
using Wonton.CrossUI.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Wonton.CrossUI.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FPGAController : ControllerBase
    {
        private readonly ILogger<FPGAController> _logger;
        private readonly FPGAManager _manager;

        public FPGAController(ILogger<FPGAController> logger, FPGAManager manager)
        {
            _logger = logger;
            _manager = manager;
        }

        [HttpGet("initio")]
        public FPGAResponse InitIO(int writeCount, int readCount)
        {
            var b = _manager.Board;
            b.InitIO(writeCount, readCount);
            return new FPGAResponse()
            {
                Message = "成功",
                Status = true
            };
        }

        [HttpGet("program")]
        public FPGAResponse Program(string bitfile)
        {
            var b = _manager.Board;
            try
            {
                var r = b.Program(bitfile);
            }
            catch (Exception e)
            {
                Electron.Notification.Show(new NotificationOptions("馄饨FPGA","Program失败"));
                return new FPGAResponse()
                {
                    Message = e.Message,
                    Status = false
                };
            }
            Electron.Notification.Show(new NotificationOptions("馄饨FPGA", "Program成功"));
            return new FPGAResponse()
            {
                Message = "成功",
                Status = true
            };
        }

        [HttpGet("ioopen")]
        public FPGAResponse IoOpen()
        {
            var b = _manager.Board;
            try
            {
                var r = b.IoOpen();
            }
            catch (Exception e)
            {
                return new FPGAResponse()
                {
                    Message = e.Message,
                    Status = false
                };
            }
            return new FPGAResponse()
            {
                Message = "成功",
                Status = true
            };
        }

        [HttpGet("ioclose")]
        public FPGAResponse IoClose()
        {
            var b = _manager.Board;
            try
            {
                var r = b.IoClose();
            }
            catch (Exception e)
            {
                return new FPGAResponse()
                {
                    Message = e.Message,
                    Status = false
                };
            }
            return new FPGAResponse()
            {
                Message = "成功",
                Status = true
            };
        }

        [HttpPost("writereadfpga")]
        public FPGAResponse WriteReadFPGA([FromBody] ushort[] write)
        {
            var b = _manager.Board;
            b.WriteBuffer.Span.Clear();
            write.CopyTo(b.WriteBuffer.Span);

            try
            {
                b.WriteReadData();
            }
            catch (Exception e)
            {
                return new FPGAResponse()
                {
                    Message = e.Message,
                    Status = false
                };
            }
            return new FPGAResponse()
            {
                Message = "成功",
                Status = true,
                Data = b.ReadBuffer.ToArray()
            };
        }

        [HttpGet("setprojectfile")]
        public FPGAResponse SetProjectFile(string filename)
        {
            _manager.CurrentProjectFile = filename;
            return new FPGAResponse()
            {
                Message = filename,
                Status = true,
                ProjectPath = filename
            };
        }

        /// <summary>
        /// 初始化程序
        /// 如果CurrentProjectFile提供了hwproj文件路径,则读取这个文件并把数据返回给UI
        /// 如果CurrentProjectFile没有提供hwproj文件路径则不读取文件,UI显示开始页面
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        [HttpGet("init")]
        public async Task<FPGAResponse> Init()
        {
            var filename = _manager.CurrentProjectFile;

            if (string.IsNullOrEmpty(filename))
            {
                //没有项目文件
                return new FPGAResponse()
                {
                    Message = "",
                    Status = false
                };
            }

            var fs = System.IO.File.Open(filename, FileMode.OpenOrCreate);
            var sr = new StreamReader(fs);
            var content = await sr.ReadToEndAsync();

            sr.Close();
            fs.Close();

            return new FPGAResponse()
            {
                Message = content,
                Status = true,
                ProjectPath = filename
            };
        }

        [HttpPost("writejson")]
        public async Task<FPGAResponse> WriteJson([FromQuery]string filename, [FromBody]ProjectInfo data)
        {
            await System.IO.File.WriteAllTextAsync(filename, data.data);

            Electron.Notification.Show(new NotificationOptions("馄饨FPGA", "已保存"));

            return new FPGAResponse()
            {
                Message = "成功",
                Status = true
            };
        }

        [HttpGet("readxmltojson")]
        public FPGAResponse ReadXmlToJson(string filename)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(filename);

            XmlNode design = xml.SelectSingleNode("design");
            var json = JsonConvert.SerializeXmlNode(design);

            return new FPGAResponse()
            {
                Message = json,
                Status = true
            };
        }

        [HttpGet("newproject")]
        public async Task<FPGAResponse> NewProject(string projectdir, string projectname, string projectiofile)
        {
            var fullpath = Path.Combine(projectdir, projectname + ".hwproj");

            XmlDocument xml = new XmlDocument();
            xml.Load(projectiofile);

            JObject jports = new JObject();

            XmlNode design = xml.SelectSingleNode("design");
            var ports = design.ChildNodes;

            for (int i = 0; i < ports.Count; i++)
            {
                var p = ports[i];
                var attr = p.Attributes;
                var name = attr.GetNamedItem("name").Value;
                var pos = attr.GetNamedItem("position").Value;

                jports.Add(name, pos);
            }

            JObject pj = new JObject();
            pj.Add("subscribedInstances",new JObject());
            pj.Add("hardwarePortsMap", new JObject());
            pj.Add("inputPortsMap", new JObject());
            pj.Add("projectInstancePortsMap", new JObject());
            pj.Add("layout",new JArray());
            pj.Add("projectPortsMap", jports);

            pj.Add("bitfile", "");
            pj.Add("projectName", projectname);

            var json = pj.ToString();
            await System.IO.File.WriteAllTextAsync(fullpath, json);

            _manager.CurrentProjectFile = fullpath;

            return new FPGAResponse()
            {
                Message = fullpath,
                Status = true,
                ProjectPath = fullpath
            };
        }
    }
}