using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Management;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Collections;
using System.Diagnostics;
using Telegram.Bot.Types.ReplyMarkups;
using Message = Telegram.Bot.Types.Message;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Telegram.Bot.Types.InputFiles;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using AForge.Video;

namespace Bot
{

    class Visitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware hw in hardware.SubHardware)
            {
                hw.Accept(this);
            }
        }

        public void VisitParameter(IParameter parameter)
        {
        }

        public void VisitSensor(ISensor sensor)
        {
        }
    }

    class Program
    {
        static FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        static VideoCaptureDevice videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
       

        public static void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            
            
            var bmp = new Bitmap(eventArgs.Frame);

            bmp.Save(@"D:/C#/test_winforms/test_winforms/screenshots/camera_01.png", ImageFormat.Png);

            videoSource.SignalToStop();

        }

        
        static string GetGPUName()
        {
            foreach (var mo in new ManagementObjectSearcher("root\\cimv2", "select * from win32_videocontroller").Get())
            {
                return mo["name"].ToString();
            }
            return "0";
        }
        static string GetCPUName()
        {
            foreach (var mo in new ManagementObjectSearcher("root\\cimv2", "select * from win32_processor").Get())
            {
                return mo["name"].ToString();
            }
            return "0";
        }
        static string GetCPUStat()
        {
            Visitor visitor = new Visitor();
            Computer computer = new Computer();
            computer.Open();
            computer.CPUEnabled = true;
            computer.Accept(visitor);
            string result = string.Empty;
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        result += $"{computer.Hardware[i].Sensors[j].Name} " +
                           $"{computer.Hardware[i].Sensors[j].SensorType}: " +
                           $" {Math.Round(Convert.ToDouble(computer.Hardware[i].Sensors[j].Value), 1)}%\n";
                    }
                }
            }
            computer.Close();
            return result;

        }

        static string GetRAMStat()
        {
            Visitor visitor = new Visitor();
            Computer computer = new Computer();
            computer.Open();
            computer.RAMEnabled = true;
            computer.Accept(visitor);
            string result = string.Empty;
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.RAM)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        switch (computer.Hardware[i].Sensors[j].SensorType)
                        {
                            case SensorType.Load:
                                result += $"{computer.Hardware[i].Sensors[j].Name} " +
                                    $"{computer.Hardware[i].Sensors[j].SensorType}: " +
                                    $" {Math.Round(Convert.ToDouble(computer.Hardware[i].Sensors[j].Value), 1)}%\n"; break;
                            case SensorType.Data:
                                result += $"{computer.Hardware[i].Sensors[j].Name} " +
                            $"{computer.Hardware[i].Sensors[j].SensorType}: " +
                            $" {Convert.ToInt32((computer.Hardware[i].Sensors[j].Value) * 1024)}Mb\n"; break;
                        }
                    }
                }
            }
            computer.Close();
            return result;

        }
        static string GetGPUStat()
        {
            Visitor visitor = new Visitor();
            Computer computer = new Computer();
            computer.Open();
            computer.GPUEnabled = true;
            computer.Accept(visitor);
            string result = string.Empty;
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.GpuNvidia)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        switch (computer.Hardware[i].Sensors[j].SensorType)
                        {
                            case SensorType.Clock:
                                {
                                    if (computer.Hardware[i].Sensors[j].Name == "GPU Shader")
                                    {
                                        break;
                                    }
                                    result += $"{computer.Hardware[i].Sensors[j].Name} " +
                                $"{computer.Hardware[i].Sensors[j].SensorType}: " +
                                $" {Convert.ToInt32(computer.Hardware[i].Sensors[j].Value)}Mhz\n";
                                    break;
                                }
                            case SensorType.Temperature:
                                {

                                    result += $"{computer.Hardware[i].Sensors[j].Name} " +
                                $"{computer.Hardware[i].Sensors[j].SensorType}: " +
                                $" {Convert.ToInt32(computer.Hardware[i].Sensors[j].Value)}°C\n";
                                    break;
                                }
                            case SensorType.Load:
                                {

                                    if (computer.Hardware[i].Sensors[j].Name == "GPU Video Engine"
                                        || computer.Hardware[i].Sensors[j].Name == "GPU Memory")
                                    {
                                        break;
                                    }
                                    result += $"{computer.Hardware[i].Sensors[j].Name} " +
                                $"{computer.Hardware[i].Sensors[j].SensorType}: " +
                                $" {Convert.ToInt32(computer.Hardware[i].Sensors[j].Value)}%\n";
                                    break;
                                }
                            case SensorType.SmallData:
                                {
                                    result += $"{computer.Hardware[i].Sensors[j].Name}: " +
                            $" {Convert.ToInt32(computer.Hardware[i].Sensors[j].Value)}Mb\n";
                                    break;

                                }


                        }

                    }
                }
            }
            computer.Close();
            return result;
        }


        static ITelegramBotClient bot = new TelegramBotClient("token");

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Некоторые действия

            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                var chatName = message.Chat.FirstName;


                if (message?.Type == MessageType.Text)
                {
                    switch (message.Text.ToLower())
                    {
                        case "keyboard":
                            {
                                ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
                                {
                                new KeyboardButton[] {"Show statistics","Delete Keyboard" },
                                new KeyboardButton[] {  "Turn off","Reboot", "Sleep" },
                                new KeyboardButton[] {"Make screenshot","Make photo" },
                            })
                                {
                                    ResizeKeyboard = true
                                };
                                Message sentMessage = await botClient.SendTextMessageAsync(
                                        chatId: message.Chat.Id,
                                        text: "choose a response: ",
                                        replyMarkup: replyKeyboardMarkup,
                                        cancellationToken: cancellationToken);
                                return;
                            }
                        case "delete keyboard":
                            {
                                Message sentMessage = await botClient.SendTextMessageAsync(
                                        chatId: message.Chat.Id,
                                        text: "Removing keyboard",
                                        replyMarkup: new ReplyKeyboardRemove(),
                                        cancellationToken: cancellationToken);
                                return;
                            }
                        case "start":
                            {
                                await botClient.SendTextMessageAsync(message.Chat, $"Glad to see you, space cowboy {chatName}. You can use these comands: start | keyboard | Delete keyboard");
                                return;
                            }
                        case "show statistics":
                            {
                                await botClient.SendTextMessageAsync(message.Chat, $"{GetCPUName()}\n" +
                                    $"\n{GetCPUStat()}\n\n{GetGPUName()}\n{GetGPUStat()}\nRAM\n{GetRAMStat()}");

                                return;
                            }
                        case "sleep":
                            {
                                await botClient.SendTextMessageAsync(message.Chat, $"Your laptop's sleeping, {chatName}");
                                Process.Start("cmd", "/c rundll32 powrprof.dll,SetSuspendState 0,1,0");
                                return;
                            }
                        case "turn off":
                            {
                                await botClient.SendTextMessageAsync(message.Chat, $"Your laptop's off, {chatName}");
                                Process.Start("cmd", "/c shutdown -s");
                                return;
                            }
                        case "reboot":
                            {
                                await botClient.SendTextMessageAsync(message.Chat, $"Your laptop's rebooted, {chatName}");
                                Process.Start("cmd", "/c shutdown -r");
                                return;
                            }
                        case "make screenshot":
                            {

                                Rectangle bounds = Screen.GetBounds(Point.Empty);
                                using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
                                {
                                    // создаем объект на котором можно рисовать
                                    using (var g = Graphics.FromImage(bitmap))
                                    {
                                        // перерисовываем экран на наш графический объект
                                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                                    }

                                    // сохраняем в файл с форматом JPG
                                    bitmap.Save(@"D:/C#/test_winforms/test_winforms/screenshots/screenshot_01.png", ImageFormat.Png);
                                }
                                string path = @"D:/C#/test_winforms/test_winforms/screenshots/screenshot_01.png";
                                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                                    await botClient.SendDocumentAsync(
                                chatId: message.Chat.Id,
                                        document: new InputOnlineFile(fileStream, fileName: path),
                                        caption: "Your screenshot :D",
                                        cancellationToken: cancellationToken);
                                return;
                            }
                        case "make photo":
                            {


                                //// set NewFrame event handler
                                //videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                                //videoSource.Start();
                                //string path = @"D:/C# projects/test_winforms/test_winforms/screenshots/camera_01.png";
                                //using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                                //    await botClient.SendDocumentAsync(
                                //chatId: message.Chat.Id,
                                //        document: new InputOnlineFile(fileStream, fileName: path),
                                //        caption: "Your photo :D",
                                //        cancellationToken: cancellationToken);
                                return;
                            }
                        default:
                            await botClient.SendTextMessageAsync(message.Chat, $"You're loh, {chatName}");
                            return;
                    }
                }
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            // Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }
        static void Main()
        {
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            Console.ReadKey();
        }

        
    }
}
