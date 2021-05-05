using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace demo.Controllers
{
    [ApiController]
    [Route("/")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        static ManualResetEventSlim _mainEvent = new ManualResetEventSlim(false);

        [HttpGet("CurrentThreads")]
        public string CurrentThreads()
        {
            return $"当前线程数：{Process.GetCurrentProcess().Threads.Count}\r\n";
        }

        [HttpGet("StartThread")]
        public string StartThread(int number)
        {
            string before = $"执行之前的线程数：{Process.GetCurrentProcess().Threads.Count}";
            for (int i = 0; i < number; i++)
            {
                new Thread(() =>
                {
                    _mainEvent.Wait();
                    _logger.LogInformation("收到信号开始处理任务");
                }).Start();
            }
            string after = $"执行之后的线程数：{Process.GetCurrentProcess().Threads.Count}";
            return $"{before}\r\n增加了{number}线程\r\n{after}\r\n";
        }

        [HttpGet("IsSet")]
        public bool IsSet()
        {
            return _mainEvent.IsSet;
        }

        [HttpGet("Set")]
        public string Set()
        {
            _mainEvent.Set();
            return $"发送set信号完成\r\n";
        }

        [HttpGet("Reset")]
        public string Reset()
        {
            _mainEvent.Reset();
            return $"发送Reset信号完成\r\n";
        }



        /// <summary>
        /// 固定超时
        /// </summary>
        static bool IsTimeout = false;
        [HttpGet("SetTimeout")]
        public ActionResult SetTimeout()
        {
            IsTimeout = !IsTimeout;
            return Ok("是否固定超时:" + IsTimeout);
        }

        /// <summary>
        /// 固定返回特定状态码
        /// </summary>
        static int ReturnCode = 200;
        [HttpGet("SetReturnCode")]
        public ActionResult SetReturnCode(int code)
        {
            ReturnCode = code;
            return Ok("已设置状态码为:" + code);
        }

        /// <summary>
        /// 健康检查接口
        /// </summary>
        /// <returns></returns>
        [HttpGet("heartbeat")]
        public ActionResult heartBeat()
        {
            // 控制返回是否超时
            string msg = "";
            if (IsTimeout)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                msg = "我处理了5秒";
                _logger.LogInformation(msg);
                return Ok(msg);
            }

            // 控制指定状态码返回
            switch (ReturnCode)
            {
                case StatusCodes.Status302Found:
                    Response.StatusCode = StatusCodes.Status302Found;
                    msg = "Status302Found";
                    _logger.LogInformation(msg);
                    return Content(msg);
                case StatusCodes.Status404NotFound:
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    msg = "Status404NotFound";
                    _logger.LogInformation(msg);
                    return Content(msg);
                case StatusCodes.Status504GatewayTimeout:
                    Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                    msg = "Status504GatewayTimeout";
                    _logger.LogInformation(msg);
                    return Content(msg);

                default:
                    break;
            }

            // 默认返回200
            Response.StatusCode = StatusCodes.Status200OK;
            msg = DateTime.Now.ToString() + " 正常的健康检查请求:" + Guid.NewGuid().ToString();
            _logger.LogInformation(msg);
            return Ok(msg);
        }


        /// <summary>
        /// 模拟消耗CPU
        /// </summary>
        /// <param name="threadCount"></param>
        /// <returns></returns>
        [HttpGet("CpuReaper")]
        public ActionResult CpuReaper(int threadCount)
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    int num = 0;
                    long start = DateTimeOffset.Now.ToUnixTimeSeconds();
                    while (true)
                    {
                        num = num + 1;
                        if (num == int.MaxValue)
                        {
                            _logger.LogInformation("reset");
                        }
                        if (DateTimeOffset.Now.ToUnixTimeSeconds() - start > 1000)
                        {
                            break;
                        }
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());
            return Ok("CpuReaper Done");
        }
    }
}
