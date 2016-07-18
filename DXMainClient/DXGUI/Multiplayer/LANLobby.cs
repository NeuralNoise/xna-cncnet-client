﻿using ClientGUI;
using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using ClientCore;
using Microsoft.Xna.Framework;
using DTAClient.domain.LAN;
using DTAClient.domain.Multiplayer;
using System.Collections.Generic;
using DTAClient.Properties;
using DTAClient.DXGUI.Generic;
using System.Net;
using System.Net.Sockets;
using Rampastring.Tools;
using DTAClient.Online;
using System.Threading;
using System.Text;
using DTAClient.domain.Multiplayer.LAN;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using System.Linq;

namespace DTAClient.DXGUI.Multiplayer
{
    class LANLobby : XNAWindow
    {
        private const double ALIVE_MESSAGE_INTERVAL = 5.0;
        private const double INACTIVITY_REMOVE_TIME = 10.0;
        private const double GAME_INACTIVITY_REMOVE_TIME = 20.0;

        public LANLobby(WindowManager windowManager, GameCollection gameCollection,
            List<GameMode> gameModes)
            : base(windowManager)
        {
            this.gameCollection = gameCollection;
            this.gameModes = gameModes;
        }

        XNAListBox lbPlayerList;
        ChatListBox lbChatMessages;
        GameListBox lbGameList;

        XNAButton btnMainMenu;
        XNAButton btnNewGame;
        XNAButton btnJoinGame;

        XNATextBox tbChatInput;

        LinkButton btnForums;
        LinkButton btnTwitter;
        LinkButton btnGooglePlus;
        LinkButton btnYoutube;
        LinkButton btnFacebook;
        LinkButton btnModDB;
        LinkButton btnHomepage;

        XNALabel lblColor;

        XNADropDown ddColor;

        LANGameCreationWindow gameCreationWindow;

        Texture2D unknownGameIcon;

        LANColor[] chatColors;

        string localGame;
        int localGameIndex;

        GameCollection gameCollection;

        List<GenericHostedGame> hostedGames = new List<GenericHostedGame>();

        List<GameMode> gameModes;

        TimeSpan timeSinceGameRefresh = TimeSpan.Zero;

        ToggleableSound sndGameCreated;

        Socket socket;
        IPEndPoint endPoint;
        Encoding encoding;

        List<LANLobbyUser> players = new List<LANLobbyUser>();

        TimeSpan timeSinceAliveMessage = TimeSpan.Zero;

        LANGameLobby lanGameLobby;

        bool initSuccess = false;

        public override void Initialize()
        {
            Name = "LANLobby";
            BackgroundTexture = AssetLoader.LoadTexture("cncnetlobbybg.png");
            ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX - 64,
                WindowManager.RenderResolutionY - 64);

            localGame = DomainController.Instance().GetDefaultGame();
            localGameIndex = gameCollection.GameList.FindIndex(
                g => g.InternalName.ToUpper() == localGame.ToUpper());

            btnNewGame = new XNAButton(WindowManager);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.ClientRectangle = new Rectangle(12, ClientRectangle.Height - 35, 133, 23);
            btnNewGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnNewGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnNewGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnNewGame.FontIndex = 1;
            btnNewGame.Text = "Create Game";
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnJoinGame = new XNAButton(WindowManager);
            btnJoinGame.Name = "btnJoinGame";
            btnJoinGame.ClientRectangle = new Rectangle(btnNewGame.ClientRectangle.Right + 12,
                btnNewGame.ClientRectangle.Y, 133, 23);
            btnJoinGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnJoinGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnJoinGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnJoinGame.FontIndex = 1;
            btnJoinGame.Text = "Join Game";
            btnJoinGame.LeftClick += BtnJoinGame_LeftClick;

            btnMainMenu = new XNAButton(WindowManager);
            btnMainMenu.Name = "btnMainMenu";
            btnMainMenu.ClientRectangle = new Rectangle(ClientRectangle.Width - 145, btnNewGame.ClientRectangle.Y,
                133, 23);
            btnMainMenu.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnMainMenu.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnMainMenu.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnMainMenu.FontIndex = 1;
            btnMainMenu.Text = "Main Menu";
            btnMainMenu.LeftClick += BtnMainMenu_LeftClick;

            btnForums = new LinkButton(WindowManager);
            btnForums.Name = "btnForums";
            btnForums.ClientRectangle = new Rectangle(ClientRectangle.Width - 33, 12, 21, 21);
            btnForums.IdleTexture = AssetLoader.LoadTexture("forumsInactive.png");
            btnForums.HoverTexture = AssetLoader.LoadTexture("forumsActive.png");
            btnForums.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnForums.URL = DomainController.Instance().GetForumURL();

            btnTwitter = new LinkButton(WindowManager);
            btnTwitter.Name = "btnTwitter";
            btnTwitter.ClientRectangle = new Rectangle(ClientRectangle.Width - 61, 12, 21, 21);
            btnTwitter.IdleTexture = AssetLoader.LoadTexture("twitterInactive.png");
            btnTwitter.HoverTexture = AssetLoader.LoadTexture("twitterActive.png");
            btnTwitter.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnTwitter.URL = DomainController.Instance().GetTwitterURL();

            btnGooglePlus = new LinkButton(WindowManager);
            btnGooglePlus.Name = "btnGooglePlus";
            btnGooglePlus.ClientRectangle = new Rectangle(ClientRectangle.Width - 89, 12, 21, 21);
            btnGooglePlus.IdleTexture = AssetLoader.LoadTexture("googlePlusInactive.png");
            btnGooglePlus.HoverTexture = AssetLoader.LoadTexture("googlePlusActive.png");
            btnGooglePlus.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnGooglePlus.URL = DomainController.Instance().GetGooglePlusURL();

            btnYoutube = new LinkButton(WindowManager);
            btnYoutube.Name = "btnYoutube";
            btnYoutube.ClientRectangle = new Rectangle(ClientRectangle.Width - 117, 12, 21, 21);
            btnYoutube.IdleTexture = AssetLoader.LoadTexture("youtubeInactive.png");
            btnYoutube.HoverTexture = AssetLoader.LoadTexture("youtubeActive.png");
            btnYoutube.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnYoutube.URL = DomainController.Instance().GetYoutubeURL();

            btnFacebook = new LinkButton(WindowManager);
            btnFacebook.Name = "btnFacebook";
            btnFacebook.ClientRectangle = new Rectangle(ClientRectangle.Width - 145, 12, 21, 21);
            btnFacebook.IdleTexture = AssetLoader.LoadTexture("facebookInactive.png");
            btnFacebook.HoverTexture = AssetLoader.LoadTexture("facebookActive.png");
            btnFacebook.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnFacebook.URL = DomainController.Instance().GetFacebookURL();

            btnModDB = new LinkButton(WindowManager);
            btnModDB.Name = "btnModDB";
            btnModDB.ClientRectangle = new Rectangle(ClientRectangle.Width - 173, 12, 21, 21);
            btnModDB.IdleTexture = AssetLoader.LoadTexture("moddbInactive.png");
            btnModDB.HoverTexture = AssetLoader.LoadTexture("moddbActive.png");
            btnModDB.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnModDB.URL = DomainController.Instance().GetModDBURL();

            btnHomepage = new LinkButton(WindowManager);
            btnHomepage.Name = "btnHomepage";
            btnHomepage.ClientRectangle = new Rectangle(ClientRectangle.Width - 201, 12, 21, 21);
            btnHomepage.IdleTexture = AssetLoader.LoadTexture("homepageInactive.png");
            btnHomepage.HoverTexture = AssetLoader.LoadTexture("homepageActive.png");
            btnHomepage.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnHomepage.URL = DomainController.Instance().GetHomepageURL();

            lbGameList = new GameListBox(WindowManager, hostedGames, localGame);
            lbGameList.Name = "lbGameList";
            lbGameList.ClientRectangle = new Rectangle(btnNewGame.ClientRectangle.X,
                41, btnJoinGame.ClientRectangle.Right - btnNewGame.ClientRectangle.X,
                btnNewGame.ClientRectangle.Top - 53);
            lbGameList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbGameList.DoubleLeftClick += LbGameList_DoubleLeftClick;
            lbGameList.AllowMultiLineItems = false;

            lbPlayerList = new XNAListBox(WindowManager);
            lbPlayerList.Name = "lbPlayerList";
            lbPlayerList.ClientRectangle = new Rectangle(ClientRectangle.Width - 202,
                lbGameList.ClientRectangle.Y, 190,
                lbGameList.ClientRectangle.Height);
            lbPlayerList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbPlayerList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbPlayerList.LineHeight = 16;

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = "lbChatMessages";
            lbChatMessages.ClientRectangle = new Rectangle(lbGameList.ClientRectangle.Right + 9,
                lbGameList.ClientRectangle.Y,
                lbPlayerList.ClientRectangle.Left - lbGameList.ClientRectangle.Right - 18,
                lbGameList.ClientRectangle.Height);
            lbChatMessages.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;

            tbChatInput = new XNATextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.X,
                btnNewGame.ClientRectangle.Y, lbChatMessages.ClientRectangle.Width,
                btnNewGame.ClientRectangle.Height);
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            lblColor = new XNALabel(WindowManager);
            lblColor.Name = "lblColor";
            lblColor.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.X, 14, 0, 0);
            lblColor.FontIndex = 1;
            lblColor.Text = "YOUR COLOR:";

            ddColor = new XNADropDown(WindowManager);
            ddColor.Name = "ddColor";
            ddColor.ClientRectangle = new Rectangle(lblColor.ClientRectangle.X + 95, 12,
                150, 21);
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;
            ddColor.ClickSoundEffect = AssetLoader.LoadSound("dropdown.wav");

            chatColors = new LANColor[]
            {
                new LANColor("Gray", Color.Gray),
                new LANColor("Metalic", Color.LightGray),
                new LANColor("Green", Color.Green),
                new LANColor("Lime Green", Color.LimeGreen),
                new LANColor("Green Yellow", Color.GreenYellow),
                new LANColor("Goldenrod", Color.Goldenrod),
                new LANColor("Yellow", Color.Yellow),
                new LANColor("Orange", Color.Orange),
                new LANColor("Red", Color.Red),
                new LANColor("Pink", Color.DeepPink),
                new LANColor("Purple", Color.MediumPurple),
                new LANColor("Sky Blue", Color.SkyBlue),
                new LANColor("Blue", Color.Blue),
                new LANColor("Brown", Color.SaddleBrown),
                new LANColor("Teal", Color.Teal)
            };

            foreach (LANColor color in chatColors)
            {
                ddColor.AddItem(color.Name, color.XNAColor);
            }

            AddChild(btnNewGame);
            AddChild(btnJoinGame);
            AddChild(btnMainMenu);

            AddChild(btnForums);
            AddChild(btnTwitter);
            AddChild(btnGooglePlus);
            AddChild(btnYoutube);
            AddChild(btnFacebook);
            AddChild(btnModDB);
            AddChild(btnHomepage);
            AddChild(lbPlayerList);
            AddChild(lbChatMessages);
            AddChild(lbGameList);
            AddChild(tbChatInput);
            AddChild(lblColor);
            AddChild(ddColor);

            gameCreationWindow = new LANGameCreationWindow(WindowManager);
            var gameCreationPanel = new DarkeningPanel(WindowManager);
            AddChild(gameCreationPanel);
            gameCreationPanel.AddChild(gameCreationWindow);
            gameCreationWindow.Disable();

            gameCreationWindow.NewGame += GameCreationWindow_NewGame;
            gameCreationWindow.LoadGame += GameCreationWindow_LoadGame;

            unknownGameIcon = AssetLoader.TextureFromImage(Resources.unknownicon);

            SoundEffect gameCreatedSoundEffect = AssetLoader.LoadSound("gamecreated.wav");

            if (gameCreatedSoundEffect != null)
                sndGameCreated = new ToggleableSound(gameCreatedSoundEffect.CreateInstance());

            encoding = Encoding.UTF8;

            base.Initialize();

            CenterOnParent();
            gameCreationPanel.SetPositionAndSize();

            lanGameLobby = new LANGameLobby(WindowManager, "MultiplayerGameLobby",
                null, gameModes, chatColors);
            DarkeningPanel dp = new DarkeningPanel(WindowManager);
            WindowManager.AddAndInitializeControl(dp);
            dp.AddChild(lanGameLobby);
            lanGameLobby.Disable();

            int selectedColor = DomainController.Instance().GetCnCNetChatColor();

            ddColor.SelectedIndex = selectedColor >= ddColor.Items.Count || selectedColor < 0
                ? 0 : selectedColor;

            lanGameLobby.GameLeft += LanGameLobby_GameLeft;
            lanGameLobby.GameBroadcast += LanGameLobby_GameBroadcast;
        }

        private void LanGameLobby_GameBroadcast(object sender, GameBroadcastEventArgs e)
        {
            SendMessage(e.Message);
        }

        private void LanGameLobby_GameLeft(object sender, EventArgs e)
        {
            Enable();
        }

        private void GameCreationWindow_LoadGame(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void GameCreationWindow_NewGame(object sender, EventArgs e)
        {
            Socket glSocket;

            try
            {
                lbChatMessages.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                    "Creating game.."));
                glSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                glSocket.EnableBroadcast = true;
                glSocket.Bind(new IPEndPoint(IPAddress.Any, ProgramConstants.LAN_GAME_LOBBY_PORT));
            }
            catch (Exception ex)
            {
                lbChatMessages.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                    "Creating socket for game lobby failed! Error message: " + ex.Message));
                return;
            }

            lanGameLobby.SetUp(true, 
                new IPEndPoint(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT), null);

            lanGameLobby.Enable();
        }

        private void DdColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbChatInput.TextColor = chatColors[ddColor.SelectedIndex].XNAColor;
            lanGameLobby.SetChatColorIndex(ddColor.SelectedIndex);
        }

        public void Open()
        {
            players.Clear();
            lbPlayerList.Clear();
            hostedGames.Clear();
            lbGameList.Clear();

            Visible = true;
            Enabled = true;

            Logger.Log("Creating LAN socket.");

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.EnableBroadcast = true;
                socket.Bind(new IPEndPoint(IPAddress.Any, ProgramConstants.LAN_LOBBY_PORT));
                endPoint = new IPEndPoint(IPAddress.Broadcast, ProgramConstants.LAN_LOBBY_PORT);
                initSuccess = true;
            }
            catch (Exception ex)
            {
                Logger.Log("Creating LAN socket failed! Message: " + ex.Message);
                lbChatMessages.AddMessage(new ChatMessage(null, Color.Red, DateTime.Now,
                    "Creating LAN socket failed! Message: " + ex.Message));
                lbChatMessages.AddMessage(new ChatMessage(null, Color.Red, DateTime.Now,
                    "Please check your firewall settings."));
                lbChatMessages.AddMessage(new ChatMessage(null, Color.Red, DateTime.Now,
                    "Also make sure that no other application is listening to traffic on UDP ports 1232 - 1234."));
                initSuccess = false;
                return;
            }

            Logger.Log("Starting listener.");
            new Thread(new ThreadStart(Listen)).Start();

            SendAlive();
        }

        private void Close()
        {
            Visible = false;
            Enabled = false;
            SendMessage("QUIT");
            socket.Close();
        }

        private void SendMessage(string message)
        {
            if (!initSuccess)
                return;

            byte[] buffer;

            buffer = encoding.GetBytes(message);

            socket.SendTo(buffer, endPoint);
        }

        private void Listen()
        {
            try
            {
                while (true)
                {
                    EndPoint ep = new IPEndPoint(IPAddress.Any, ProgramConstants.LAN_LOBBY_PORT);
                    byte[] buffer = new byte[4096];
                    int receivedBytes = 0;
                    receivedBytes = socket.ReceiveFrom(buffer, ref ep);

                    IPEndPoint iep = (IPEndPoint)ep;

                    string data = encoding.GetString(buffer, 0, receivedBytes);

                    if (data == string.Empty)
                        continue;

                    AddCallback(new Action<string, IPEndPoint>(HandleNetworkMessage), data, iep);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("LAN socket listener: exception: " + ex.Message);
            }
        }

        private void HandleNetworkMessage(string data, IPEndPoint endPoint)
        {
            string[] commandAndParams = data.Split(' ');

            if (commandAndParams.Length < 2)
                return;

            string command = commandAndParams[0];

            string[] parameters = data.Substring(command.Length + 1).Split(
                new char[] { ProgramConstants.LAN_DATA_SEPARATOR },
                StringSplitOptions.RemoveEmptyEntries);

            LANLobbyUser user = players.Find(p => p.EndPoint.Equals(endPoint));

            switch (command)
            {
                case "ALIVE":
                    if (parameters.Length < 2)
                        return;

                    int gameIndex = Conversions.IntFromString(parameters[0], -1);
                    string name = parameters[1];

                    if (user == null)
                    {
                        Texture2D gameTexture = unknownGameIcon;

                        if (gameIndex > -1 && gameIndex < gameCollection.GameList.Count)
                            gameTexture = gameCollection.GameList[gameIndex].Texture;

                        user = new LANLobbyUser(name, gameTexture, endPoint);
                        players.Add(user);
                        lbPlayerList.AddItem(user.Name, gameTexture);
                    }

                    user.TimeWithoutRefresh = TimeSpan.Zero;

                    break;
                case "CHAT":
                    if (user == null)
                        return;

                    if (parameters.Length < 2)
                        return;

                    int colorIndex = Conversions.IntFromString(parameters[0], -1);

                    if (colorIndex < 0 || colorIndex >= chatColors.Length)
                        return;

                    lbChatMessages.AddMessage(new ChatMessage(user.Name, 
                        chatColors[colorIndex].XNAColor, DateTime.Now, parameters[1]));

                    break;
                case "QUIT":
                    if (user == null)
                        return;

                    int index = players.FindIndex(p => p == user);

                    players.RemoveAt(index);
                    lbPlayerList.Items.RemoveAt(index);
                    break;
                case "GAME":
                    if (user == null)
                        return;

                    HostedLANGame game = new HostedLANGame();
                    if (!game.SetDataFromStringArray(gameCollection, parameters))
                        return;
                    game.EndPoint = endPoint;

                    int existingGameIndex = hostedGames.FindIndex(g => ((HostedLANGame)g).EndPoint.Equals(endPoint));

                    if (existingGameIndex > -1)
                        hostedGames[existingGameIndex] = game;
                    else
                    {
                        hostedGames.Add(game);
                    }

                    lbGameList.Refresh();

                    break;
            }
        }

        private void SendAlive()
        {
            StringBuilder sb = new StringBuilder("ALIVE ");
            sb.Append(localGameIndex);
            sb.Append(ProgramConstants.LAN_DATA_SEPARATOR);
            sb.Append(ProgramConstants.PLAYERNAME);
            SendMessage(sb.ToString());
            timeSinceAliveMessage = TimeSpan.Zero;
        }

        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            string chatMessage = tbChatInput.Text.Replace((char)01, '?');

            StringBuilder sb = new StringBuilder("CHAT ");
            sb.Append(ddColor.SelectedIndex);
            sb.Append(ProgramConstants.LAN_DATA_SEPARATOR);
            sb.Append(chatMessage);

            SendMessage(sb.ToString());

            tbChatInput.Text = string.Empty;
        }

        private void LbGameList_DoubleLeftClick(object sender, EventArgs e)
        {
            if (lbGameList.SelectedIndex < 0 || lbGameList.SelectedIndex >= lbGameList.Items.Count)
                return;

            HostedLANGame hg = (HostedLANGame)lbGameList.Items[lbGameList.SelectedIndex].Tag;

            if (hg.Game.InternalName.ToUpper() != localGame.ToUpper())
            {
                lbChatMessages.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                    "The selected game is for " +
                    gameCollection.GetGameNameFromInternalName(hg.Game.InternalName) + "!"));
                return;
            }

            if (hg.Players.Contains(ProgramConstants.PLAYERNAME))
            {
                lbChatMessages.AddMessage(null, "Your name is already taken in the game.", Color.White);
                return;
            }

            if (hg.Locked)
            {
                lbChatMessages.AddMessage(null, "The selected game is locked!", Color.White);
                return;
            }

            if (hg.IsLoadedGame)
            {
                if (!hg.Players.Contains(ProgramConstants.PLAYERNAME))
                {
                    lbChatMessages.AddMessage(null, "You do not exist in the saved game!", Color.White);
                    return;
                }
            }

            if (hg.GameVersion != ProgramConstants.GAME_VERSION)
            {
                // TODO Show warning
            }

            int gameId = -1;

            lbChatMessages.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                "Attempting to join game " + hg.RoomName + "..."));

            if (hg.IsLoadedGame)
            {
                throw new NotImplementedException();
            }

            try
            {
                var client = new TcpClient(hg.EndPoint.Address.ToString(), ProgramConstants.LAN_GAME_LOBBY_PORT);

                lanGameLobby.SetUp(false, hg.EndPoint, client);
                lanGameLobby.Enable();

                var buffer = encoding.GetBytes("JOIN" + ProgramConstants.LAN_DATA_SEPARATOR + ProgramConstants.PLAYERNAME);

                client.GetStream().Write(buffer, 0, buffer.Length);
                client.GetStream().Flush();
            }
            catch (Exception ex)
            {
                lbChatMessages.AddMessage(null,
                    "Connecting to the game failed! Message: " + ex.Message, Color.White);
            }
        }

        private void BtnMainMenu_LeftClick(object sender, EventArgs e)
        {
            Close();
        }

        private void BtnJoinGame_LeftClick(object sender, EventArgs e)
        {
            LbGameList_DoubleLeftClick(this, EventArgs.Empty);
        }

        private void BtnNewGame_LeftClick(object sender, EventArgs e)
        {
            gameCreationWindow.Open();
        }

        public override void Update(GameTime gameTime)
        {
            for (int i = 0; i < players.Count; i++)
            {
                players[i].TimeWithoutRefresh += gameTime.ElapsedGameTime;

                if (players[i].TimeWithoutRefresh > TimeSpan.FromSeconds(INACTIVITY_REMOVE_TIME))
                {
                    lbPlayerList.Items.RemoveAt(i);
                    players.RemoveAt(i);
                    i--;
                }
            }

            bool gameRemoved = false;

            for (int i = 0; i < hostedGames.Count; i++)
            {
                HostedLANGame lg = (HostedLANGame)hostedGames[i];

                lg.TimeWithoutRefresh += gameTime.ElapsedGameTime;

                if (lg.TimeWithoutRefresh > TimeSpan.FromSeconds(GAME_INACTIVITY_REMOVE_TIME))
                {
                    gameRemoved = true;
                    hostedGames.RemoveAt(i);
                    i--;
                }
            }

            timeSinceAliveMessage += gameTime.ElapsedGameTime;
            if (timeSinceAliveMessage > TimeSpan.FromSeconds(ALIVE_MESSAGE_INTERVAL))
                SendAlive();

            if (gameRemoved)
                lbGameList.Refresh();

            base.Update(gameTime);
        }
    }
}