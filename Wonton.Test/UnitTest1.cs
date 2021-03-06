using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Wonton.Common;
using Newtonsoft.Json;
using Xunit;

namespace Wonton.Test
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            FPGABoard b = new FPGABoard();
            b.InitIO(4,4);
            b.Program(@"E:\Documents\Repo\ProjectFDB\WontonInterface\Wonton.Test\AlarmClock_fde_dc.bit");

            b.IoOpen();

            for (int i = 0; i < 70; i++)
            {
                if (i < 2)
                {
                    b.WriteBuffer.Span[0] = 0x00;
                }
                else
                {
                    b.WriteBuffer.Span[0] = 0x01;
                }

                b.WriteBuffer.Span[1] = 0x00;
                b.WriteBuffer.Span[2] = 0x00;
                b.WriteBuffer.Span[3] = 0x00;

                b.ReadBuffer.Span.Clear();

                b.WriteReadData();

                var hr_out = b.ReadBuffer.Span[0] & 0x000F;
                var min_out = (b.ReadBuffer.Span[0] & 0x03F0) >> 4;
                var sec_out = (b.ReadBuffer.Span[0] & 0xFC00) >> 10;
                var hr_alarm = b.ReadBuffer.Span[1] & 0x000F;
                var min_alarm = (b.ReadBuffer.Span[1] & 0x03F0) >> 4;
                var alarm = (b.ReadBuffer.Span[1] & 0x0400) >> 10;

                Debug.WriteLine($"[{i}] hr_out[{hr_out}] min_out[{min_out}] sec_out[{sec_out}] hr_alarm[{hr_alarm}] min_alarm[{min_alarm}] alarm[{alarm}]");
            }

            b.IoClose();

        }

        [Fact]
        void TestXml()
        {
            XmlDocument xml = new XmlDocument();
            XDocument x = new XDocument();

            xml.Load(@"E:\Downloads\VeriCommSDK-2019-11-22�յ�\VeriCommSDK\Example\Alarm_Clock\FDP3P7\FDE\src\AlarmClock.xml");

            XmlNode design = xml.SelectSingleNode("design");
            var json = JsonConvert.SerializeXmlNode(design);
        }

        [Fact]
        void TestConfig()
        {
            var render_config = "target: 'electron-renderer'";
            var config_file = System.IO.Path.Combine(@"F:\Repo\WontonInterface\Wonton.CrossUI.Web\ClientApp\node_modules\react-scripts\config", "webpack.config.js");
            var config_contents = File.ReadAllLines(config_file);
            List<string> modified_contents = new List<string>();
            var modified = false;
            foreach (var line in config_contents)
            {
                if (line.Contains(render_config))
                {
                    modified = true;
                    break;
                }
            }

            if (!modified)
            {
                foreach (var line in config_contents)
                {
                    modified_contents.Add(line);
                    if (line.Contains("mode: isEnvProduction"))
                    {
                        modified_contents.Add("    " + render_config + ",");
                    }
                }

                File.WriteAllText(config_file, string.Join(Environment.NewLine, modified_contents));
            }
        }
    }
}
