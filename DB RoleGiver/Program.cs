using Discord;
using Discord.WebSocket;
using System;
using System.Xml;

namespace DB_Role_Giver 
{
    class Program 
    {
        private DiscordSocketClient _client;
        public static Task Main(string[] args) => new Program().MainAsync();

        private static Role role = new Role();

        public async Task MainAsync()
        {

            _client = new DiscordSocketClient();

            _client.Log += Log;

            _client.Ready += Client_Ready;

            _client.SlashCommandExecuted += SlashCommandHandler;

            var token = File.ReadAllText("E:\\CS_LOGS\\DB_Role_Giver\\token.txt");
            

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        public async Task Client_Ready() 
        {
            //var guild = _client.GetGuild(857945831519813672);

            var guilds = _client.Guilds.ToList();

            var gr = new SlashCommandBuilder();

            var roleList = new SlashCommandBuilder();

            roleList.WithName("role-list").WithDescription("Display the role history of this server");


            gr.WithName("give-role");
            gr.WithDescription("issuing a role and recording it in the log");
            gr.AddOption("user", ApplicationCommandOptionType.User, "the user to whom you want to add a role", isRequired: true);
            gr.AddOption("role_name", ApplicationCommandOptionType.String, "Role name", isRequired: true);
            gr.AddOption("description", ApplicationCommandOptionType.String, "Role Description", isRequired: false);
            gr.AddOption("color", ApplicationCommandOptionType.Integer, "Set role color NOT WORKING", isRequired: false);

            try
            {
                foreach (var guild in guilds) 
                {
                    await guild.CreateApplicationCommandAsync(gr.Build());
                    await guild.CreateApplicationCommandAsync(roleList.Build());
                }

            }
            catch { }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "give-role":
                    await HandlerGiveRoleCommand(command);
                    Logging(command);
                    break;
                case "role-list":
                    await HandlerRoleListCommand(command);
                    break;
            }
            
        }

        private async Task HandlerGiveRoleCommand(SocketSlashCommand command)
        {
            

            var guild = _client.GetGuild((ulong)command.GuildId);
            var options = command.Data.Options.ToList();

           // Color clr = new Color((long)options[2].Value / 100 , ((long)options[2].Value % 100) / 10 , ((long)options[2].Value % 100) % 10);

            Random random = new Random();

            Color clr = new Color((byte)random.Next(0,255),(byte)random.Next(0,255),(byte)random.Next(0,255));
            guild.CreateRoleAsync(options[1].Value.ToString(), null, clr);
          

            TimerCallback tm = new TimerCallback(Find_And_Give_Role);
            

            role_tmp tmp = new role_tmp();
            tmp.guild = guild;
            tmp.name = options[1].Value.ToString();
            tmp.user = ((Discord.IGuildUser)options[0].Value);
            Timer timer = new Timer(tm, tmp, 11000, Timeout.Infinite);

            role = new Role();
            role.user = ((Discord.IGuildUser)options[0].Value);
            role.description = options[2].Value.ToString();
            role.guild = guild;
            role.commander = guild.GetUser(command.User.Id);

            command.RespondAsync("Успешно");
        }

       
        public  void Find_And_Give_Role(object obj) 
        {
            var tmp = (role_tmp)obj;
            var guild = _client.GetGuild((ulong)tmp.guild.Id); //refresh role list
            ulong result = 0;
            guild.Roles.ToList().ForEach(rolet => { if (rolet.Name == tmp.name) result = rolet.Id;});
            role.id = result; role.guild = guild; 
            tmp.user.AddRoleAsync(result);
        }


        public async Task Logging(SocketSlashCommand command) 
        {
            await Task.Delay(11000);

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("logs.xml");
            XmlElement? xRoot = xDoc.DocumentElement;

            XmlElement serverElem = xDoc.CreateElement("server");
            XmlAttribute serverAttr = xDoc.CreateAttribute("id");
            XmlText serverText = xDoc.CreateTextNode(role.guild.Id.ToString());
       
            XmlElement roleElem = xDoc.CreateElement("role");
      
            XmlAttribute roleAttr = xDoc.CreateAttribute("id");
      
            XmlText roleText = xDoc.CreateTextNode(role.id.ToString());
    
            XmlElement userElem = xDoc.CreateElement("user");
            XmlText userText = xDoc.CreateTextNode(role.user.Id.ToString());
  
            XmlElement commanderElem = xDoc.CreateElement("commander");
            XmlText commanderText = xDoc.CreateTextNode(role.commander.Id.ToString());
 
            XmlElement dateElem = xDoc.CreateElement("date");
            XmlText dateText = xDoc.CreateTextNode(role.date.ToString());

            XmlElement descriptionElem = xDoc.CreateElement("description");
            XmlText descriptionText = xDoc.CreateTextNode(role.description);

        
            serverAttr.AppendChild(serverText);
            roleAttr.AppendChild(roleText);
    
            userElem.AppendChild(userText);
            commanderElem.AppendChild(commanderText);
            dateElem.AppendChild(dateText);
            descriptionElem.AppendChild(descriptionText);
          
            roleElem.Attributes.Append(roleAttr);
           
           
            roleElem.AppendChild(userElem);
            
            roleElem.AppendChild(commanderElem);
           
            roleElem.AppendChild(dateElem);
           
            roleElem.AppendChild(descriptionElem);
            
            serverElem.Attributes.Append(serverAttr);
            serverElem.AppendChild(roleElem);
           
            xRoot?.AppendChild(serverElem);
            
            xDoc.Save("logs.xml");
            Console.WriteLine("role added");
        }

        private async Task HandlerRoleListCommand(SocketSlashCommand command) 
        {
            string result = " ";
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("logs.xml");
            XmlElement? xRoot = xDoc.DocumentElement;
            var guildId = (ulong)command.GuildId;
            var guild = _client.GetGuild(guildId);

            foreach (XmlElement xnode in xRoot) 
            {
                var attr = xnode.Attributes.GetNamedItem("id");
                if (ulong.Parse( attr.Value ) == guildId) 
                {
                    var xmlRole = xnode.FirstChild;
                    var roleId = xmlRole.Attributes.GetNamedItem("id");
                    result += "Role: ";
                    if (roleId.Value != "0") result += guild.GetRole(ulong.Parse(roleId.Value)).Name;
                    else result += "undefined";
                    result += "\n";
                    foreach (XmlNode childNode in xmlRole.ChildNodes) 
                    {
                        result += childNode.LocalName;

                        result += ": ";
                        if (childNode.Name != "user" && childNode.Name != "commander") result += childNode.InnerText;
                        else 
                        {
                            result += guild.GetUser(ulong.Parse(childNode.InnerText));
                        }
                        result += ' ';
                        result += '\n';
                    }
                    result += '\n';
                }
            }


            command.RespondAsync(result);
        }

        private class role_tmp 
        {
            public ulong Id;
            public IGuildUser user;
            public string name;
            public SocketGuild guild;
        }

    }

    //class server 
    //{
    //    public IGuild Guild;

    //    public List<rgRole> roles;
    //}

    class Role
    {
        public ulong id;
        public string description;
        public IGuildUser user;
        public IGuildUser commander;
        public DateTime date;
        public SocketGuild guild;
        public  Role()
        {
            description = "";
            id = 0;
            date = DateTime.Now;
        }
       public Role(ulong idd, string dsp, IGuildUser usr, IGuildUser cmnd, SocketGuild gld) 
        {
            id = idd;
            description = dsp;
            user = usr;
            commander = cmnd;
            guild = gld;
            date = DateTime.Now;
        }
    }

}