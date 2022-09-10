using MaminDis.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Claims;

namespace MaminDis.Controllers
{
    public class HomeController : Controller
    {
        private static readonly List<string> registeredNames = new List<string>();
        private static List<Room> rooms = new List<Room>();
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View(rooms);
        }
        public IActionResult Login()
        {
            ViewBag.IndexUrl = Url.Action("Index");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string name)
        {
            if (registeredNames.Any(n => n == name))
            {
                return View();
            }
            var claims = new List<Claim>() {
                    new Claim(ClaimTypes.Name, name)
                };
            //Initialize a new instance of the ClaimsIdentity with the claims and authentication scheme    
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            //Initialize a new instance of the ClaimsPrincipal with ClaimsIdentity    
            var principal = new ClaimsPrincipal(identity);
            //SignInAsync is a Extension method for Sign in a principal for the specified scheme.    
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties()
            {
                IsPersistent = true
            });
            return RedirectToAction();
        }
        [HttpGet]
        [Authorize]
        [Route("/enterroom/{roomid}")]
        public IActionResult EnterRoom([FromRoute] int roomid)
        {

            var room = rooms.SingleOrDefault(room => room.Id == roomid);
            ViewBag.vcSocketUrl = $"{HttpContext.Request.Host.ToString()}/server/{roomid}";
            ViewBag.usersSocketUrl = $"{HttpContext.Request.Host.ToString()}/join/{roomid}";
            ViewBag.UpdateUsersUrl = $"{HttpContext.Request.Host.ToString()}/getusers/{roomid}";
            if (room != null)
            {
                return View("RoomView", room);
            }
            return NotFound();
        }
        [Route("/getusers/{roomId}")]
        [Authorize]
        public IActionResult GetUsers(int roomId)
        {
            try
            {
                var room = rooms.SingleOrDefault(room => room.Id == roomId);
                var res = Json(room.Members);
                return Json(room.Members);
            }
            catch
            {
                return NotFound();
            }
        }
        [Authorize]
        public IActionResult CreateRoom(string roomname)
        {
            if (roomname != null && !rooms.Any(room => room.Title == roomname) && !roomname.Contains(" "))
            {
                var room = new Room()
                {
                    Title = roomname,
                    Members = new List<User>(),
                    Id = new Utils.UniqueRandomNumberGenerator(1000, 9999).Next()
                };
                rooms.Add(room);
                return RedirectToAction("EnterRoom", new
                {
                    roomid = room.Id,
                });
            }
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



        [Authorize]
        [HttpGet]
        [Route("server/{roomid}")]
        public async Task HandleVoiceChat(int roomid)
        {
            using (var client = await HttpContext.WebSockets.AcceptWebSocketAsync())
            {
                var room = rooms.SingleOrDefault(room => room.Id == roomid);
                room!.Members?.Add(new User()
                {
                    Name = User.Identity.Name!,
                    VoiceChatSocket = client
                });
                while (client.State == WebSocketState.Open)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[10000];
                        WebSocketReceiveResult receiveResult = await client.ReceiveAsync(
                            buffer, CancellationToken.None);
                        foreach (var user in room!.Members!)
                        {
                            if (user.VoiceChatSocket != client)
                            {
                                await user.VoiceChatSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
                            }
                        }
                    }
                }
                if (client.State != WebSocketState.Open)
                {
                    room.Members?.Remove(room.Members.First(r => r.VoiceChatSocket == client));
                }
            }

        }
        [HttpGet]
        [Authorize]
        [Route("join/{roomid}")]
        public async Task JoinOrLeaveRoom(int roomid)
        {
            var room = rooms.SingleOrDefault(room => room.Id == roomid);
            using (var client = await HttpContext.WebSockets.AcceptWebSocketAsync())
            {
                room.UsersListReceivers.Add(client);
                foreach (var socket in room!.UsersListReceivers!)
                {
                    if (socket != client)
                    {
                        await socket.SendAsync(new byte[1], WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                }

                while (client.State == WebSocketState.Open)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[0];
                        WebSocketReceiveResult receiveResult = await client.ReceiveAsync(
                            buffer, CancellationToken.None);
                    }
                }
                room.UsersListReceivers.Remove(client);
                foreach (var socket in room!.UsersListReceivers!)
                {
                    await socket.SendAsync(new byte[0], WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }
        public IActionResult About()
        {
            return View();
        }

    }
}