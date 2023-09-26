using Discord;
using Discord.WebSocket;
using System;
using System.Xml;
using System.Linq;

namespace DB_Role_Giver 
{
    class Program 
    {
        private DiscordSocketClient _client;
        public static Task Main(string[] args) => new Program().MainAsync();

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

            var guilds = _client.Guilds.ToList();

            var gr = new SlashCommandBuilder();

            var roleList = new SlashCommandBuilder();

            var roleListFile = new SlashCommandBuilder();

            var findRole = new SlashCommandBuilder();

            roleList.WithName("role-list").WithDescription("Display the role history of this server");
            roleListFile.WithName("role-list-file").WithDescription("Return a txt file with the history of server roles");
            findRole.WithName("find-role").WithDescription("Find the role in the history of this server").AddOption("role", ApplicationCommandOptionType.Role, "name of the role you want to find", isRequired:true) ;


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
                    await guild.CreateApplicationCommandAsync(roleListFile.Build());
                    await guild.CreateApplicationCommandAsync(findRole.Build());
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
                    
                    break;
                case "role-list":
                    await HandlerRoleListCommand(command);
                    break;
                case "role-list-file":
                    await HandlerRoleListFileCommand(command);
                    break;
                case "find-role":
                    await HandlerFindRoleCommand(command);
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
            var new_role = await guild.CreateRoleAsync(options[1].Value.ToString(), null, clr);



 


           
            guild.GetUser(((SocketGuildUser)options[0].Value).Id).AddRoleAsync(new_role.Id);


            Role role = new Role();
            role.user = ((Discord.IGuildUser)options[0].Value);
            if (command.Data.Options.Count > 2 && options[2] != null) role.description = options[2].Value.ToString();
            else role.description = "none";
            role.guild = guild;
            role.commander = guild.GetUser(command.User.Id);
            role.id = new_role.Id;




            await Logging(command, role);
            command.RespondAsync("Successfully");
        }


        public async Task Logging(SocketSlashCommand command, Role role)
        {

            bool serverExists = false;

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("logs.xml");
            XmlElement? xRoot = xDoc.DocumentElement;
            XmlNode xmlNode = null;


            XmlElement serverElem;
            XmlAttribute serverAttr;
            XmlText serverText;


            foreach (XmlNode xNode in xRoot)
            {
                if (xNode.Name == "server" && Convert.ToUInt64(xNode.Attributes.GetNamedItem("id").Value) == role.guild.Id) { serverExists = true; xmlNode = xNode; break; }
            }


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
            XmlText descriptionText = null;
            if (role.description != null)
            {
                descriptionText = xDoc.CreateTextNode(role.description);
            }
            else descriptionText = xDoc.CreateTextNode("-");
        
           
           


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

            if (!serverExists)
            {
                serverElem = xDoc.CreateElement("server");
                serverAttr = xDoc.CreateAttribute("id");
                serverText = xDoc.CreateTextNode(role.guild.Id.ToString());
                serverAttr.AppendChild(serverText);
                serverElem.Attributes.Append(serverAttr);
                serverElem.AppendChild(roleElem);
                xRoot?.AppendChild(serverElem);
            }
            else 
            {
                xmlNode.AppendChild(roleElem);
            }
            xDoc.Save("logs.xml");
            Console.WriteLine("role added");

        }

        private async Task HandlerRoleListCommand(SocketSlashCommand command) 
        {
            string result = "";
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
                    int i = 0;
                    foreach (XmlElement xRole in xnode.ChildNodes) 
                    {
                        if (i++ < xnode.ChildNodes.Count - 5) { continue;  }
                        var roleId = xRole.Attributes.GetNamedItem("id");
                        result += "Role: ";
                        if (roleId.Value != "0" && guild.GetRole(ulong.Parse(roleId.Value)) != null) result += guild.GetRole(ulong.Parse(roleId.Value)).Name;
                        else result += "undefined";
                        result += "\n";
                        foreach (XmlNode childNode in xRole.ChildNodes)
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
                break;
            }

            var guildUser = (SocketGuildUser)command.User;
            var embedBuiler = new EmbedBuilder()
            .WithAuthor(guildUser.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
            .WithTitle("Last five roles:")
            .WithDescription(result)
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();





            command.RespondAsync(embed: embedBuiler.Build());
        }

        private async Task HandlerRoleListFileCommand(SocketSlashCommand command)
        {
            string result = "";
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("logs.xml");
            XmlElement? xRoot = xDoc.DocumentElement;
            var guildId = (ulong)command.GuildId;
            var guild = _client.GetGuild(guildId);

            foreach (XmlElement xnode in xRoot)
            {
                var attr = xnode.Attributes.GetNamedItem("id");
                if (ulong.Parse(attr.Value) == guildId)
                {
                    foreach (XmlElement xRole in xnode.ChildNodes)
                    {
                        var roleId = xRole.Attributes.GetNamedItem("id");
                        result += "Role: ";
                        if (roleId.Value != "0" && guild.GetRole(ulong.Parse(roleId.Value)) != null) result += guild.GetRole(ulong.Parse(roleId.Value)).Name;
                        else result += "undefined";
                        result += "\n";
                        foreach (XmlNode childNode in xRole.ChildNodes)
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
                break;
            }

            File.WriteAllText("lastRoleHistory.txt", result);

            command.RespondWithFileAsync("lastRoleHistory.txt");
           
        }

        private async Task HandlerFindRoleCommand(SocketSlashCommand command) 
        {
            string result = "n/a";

            string roleName = "";

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("logs.xml");

            XmlElement? xRoot = xDoc.DocumentElement;

            var guildId = (ulong)command.GuildId;
            var guild = _client.GetGuild(guildId);


            foreach (XmlElement xNode in xRoot) 
            {
                if (ulong.Parse(xNode.Attributes.GetNamedItem("id").Value) == guildId) 
                {
                    foreach (XmlNode xRole in xNode) 
                    {
                        if (ulong.Parse(xRole.Attributes.GetNamedItem("id").Value) == ((SocketRole)(command.Data.Options.First().Value)).Id) 
                        {
                            roleName = guild.GetRole(ulong.Parse(xRole.Attributes.GetNamedItem("id").Value)).Name;
                            result = "";
                            foreach (XmlNode childNode in xRole.ChildNodes)
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
                            break;
                        }
                    }

                    break;
                }
                
            }


            var guildUser = (SocketGuildUser)command.User;
            var embedBuiler = new EmbedBuilder()
            .WithAuthor(guildUser.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
            .WithTitle(roleName)
            .WithDescription(result)
            .WithColor(Color.Green)
            .WithCurrentTimestamp();





            command.RespondAsync(embed: embedBuiler.Build());
        }






    }


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
       public Role(ulong id, string description, IGuildUser user, IGuildUser commander, SocketGuild guild) 
        {
            this.id = id;
            this.description = description;
            this.user = user;
            this.commander = commander;
            this.guild = guild;
            this.date = DateTime.Now;
        }
    }

}