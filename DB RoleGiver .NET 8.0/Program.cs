using Discord;
using Discord.WebSocket;
using System;
using System.Xml;
using System.Data;
using System.Drawing;
using System.Globalization;


namespace DB_Role_Giver
{
    class Program
    {
        private DiscordSocketClient _client;
        public static Task Main() => new Program().MainAsync();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;

            _client.Ready += Client_Ready;

            _client.SlashCommandExecuted += SlashCommandHandler;

            _client.JoinedGuild += JoinedGuild;
            try
            {
                string path = File.ReadAllText("path.TXT");

                var token = File.ReadAllText(path);


                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task JoinedGuild(SocketGuild guild)
        {
            AddSlashCommandsToGuild(guild);
        }

        public async Task Client_Ready()
        {

            var guilds = _client.Guilds.ToList();

            

            try
            {
                foreach (var guild in guilds)
                {
                    if ((await guild.GetApplicationCommandsAsync()).FirstOrDefault(x => x.Name == "give-role") == null)
                    {
                        AddSlashCommandsToGuild(guild);
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
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
                case "role-remove":
                    await HandlerRoleRemoveCommand(command);
                    break;
                case "clear-mode":
                    await HandlerClearModeCommand(command);
                    break;
                case "russian-mode":
                    command.RespondAsync("Данная команда была удалена");
                    break;
                case "all-role-add":
                    await HandlerAllRoleAddCommand(command);
                    break;
                case "role-modify":
                    await HandlerRoleModifyAddCommand(command);
                    break;
                case "remove-miss-roles":
                    await HandlerRemoveMissRolesCommand(command);
                    break;
            }

        }

        private async Task HandlerRemoveMissRolesCommand(SocketSlashCommand command)
        {

            string err;
            if ((err = PermisionsCheck(command.GuildId, command.User.Id)) != null)
            {
                command.RespondAsync(err);
                return;
            }

            var guild = _client.GetGuild((ulong)command.GuildId);





            XmlDocument xDoc = ErrorProcessing.OpenXDoc("logs.xml");

            XmlElement? xRoot = xDoc.DocumentElement;

            foreach (XmlElement xNode in xRoot)
            {
                if (ulong.Parse(xNode.Attributes.GetNamedItem("id").Value) == command.GuildId)
                {
                    foreach (XmlNode xRole in xNode)
                    {
                        if (guild.GetRole(ulong.Parse(xRole.Attributes.GetNamedItem("id").Value)) == null) xNode.RemoveChild(xRole);
                    }
                }
            }


            xDoc.Save("logs.xml");
            command.RespondAsync("Successfully");
        }

        private async Task HandlerRoleModifyAddCommand(SocketSlashCommand command)
        {
            string err;
            if ((err = PermisionsCheck(command.GuildId, command.User.Id)) != null)
            {
                command.RespondAsync(err);
                return;
            }

            XmlDocument xDoc = ErrorProcessing.OpenXDoc("logs.xml");

            XmlElement? xRoot = xDoc.DocumentElement;

            var guildId = (ulong)command.GuildId;



            foreach (XmlElement xNode in xRoot)
            {
                if (ulong.Parse(xNode.Attributes.GetNamedItem("id").Value) == guildId)
                {

                    foreach (XmlNode xRole in xNode)
                    {
                        if (ulong.Parse(xRole.Attributes.GetNamedItem("id").Value) == ((SocketRole)(command.Data.Options.First().Value)).Id)
                        {
                            XmlNode xDescription = xRole.ChildNodes.Item(3);
                            xDescription.InnerText = (string)(command.Data.Options.Last().Value);
                        }
                    }

                    break;
                }


            }
            xDoc.Save("logs.xml");
            command.RespondAsync("Successfully");
        }


        private async Task HandlerAllRoleAddCommand(SocketSlashCommand command)
        {
            string err;
            if ((err = PermisionsCheck(command.GuildId, command.User.Id)) != null)
            {
                command.RespondAsync(err);
                return;
            }
            Role role;
            var guild = _client.GetGuild((ulong)command.GuildId);
            foreach (var oldRole in guild.Roles)
            {
                if (oldRole.Members != null && oldRole.Members.Any()) role = new Role(oldRole.Id, "none", oldRole.Members.Where(x => x != null).First(), oldRole.Members.Where(x => x != null).First(), guild, oldRole.CreatedAt);
                else role = new Role(oldRole.Id, "none", null, null, guild, oldRole.CreatedAt);
                if (!await CheckRolePresence(oldRole.Id, guild.Id)) Logging(role);
            }
            await command.RespondAsync("Successfully");
        }

        private async Task<bool> CheckRolePresence(ulong roleId, ulong serverId)
        {
            bool result = false;
            XmlDocument xDoc = ErrorProcessing.OpenXDoc("logs.xml");
            XmlElement xRoot = xDoc.DocumentElement;
            foreach (XmlElement xServer in xRoot)
            {
                if (ulong.Parse(xServer.Attributes.GetNamedItem("id").Value) == serverId)
                {
                    foreach (XmlNode xRole in xServer.ChildNodes)
                    {
                        if (ulong.Parse(xRole.Attributes.GetNamedItem("id").Value) == roleId)
                        {
                            result = true;
                            break;
                        }
                    }
                    break;
                }
            }

            return result;
        }

        private async Task HandlerClearModeCommand(SocketSlashCommand command)
        {
            bool isOn = false;
            XmlDocument xDoc = ErrorProcessing.OpenXDoc("logs.xml");
            

            XmlElement xRoot = xDoc.DocumentElement;

            foreach (XmlNode xNode in xRoot)
            {
                if (xNode.Name == "server" && Convert.ToUInt64(xNode.Attributes.GetNamedItem("id").Value) == command.GuildId)
                {
                    if (xNode.Attributes.GetNamedItem("mode") != null)
                    {
                        if (xNode.Attributes.GetNamedItem("mode").Value != "on") { xNode.Attributes.GetNamedItem("mode").Value = "on"; isOn = true; }
                        else xNode.Attributes.GetNamedItem("mode").Value = "off";
                    }
                    else
                    {
                        var xAttr = xDoc.CreateAttribute("mode");
                        xAttr.AppendChild(xDoc.CreateTextNode("on"));
                        xNode.Attributes.Append(xAttr);
                        isOn = true;
                    }
                }

            }
            xDoc.Save("logs.xml");
            if (isOn) await command.RespondAsync("Clear mode enable");
            else await command.RespondAsync("Clear mode disable");
        }


        private async Task HandlerGiveRoleCommand(SocketSlashCommand command)
        {
            string err;
            if ((err = PermisionsCheck(command.GuildId, command.User.Id)) != null)
            {
                command.RespondAsync(err);
                return;
            }
            string roleDescription = "none";

            var guild = _client.GetGuild((ulong)command.GuildId);
            var options = command.Data.Options.ToList();

            // Color clr = new Color((long)options[2].Value / 100 , ((long)options[2].Value % 100) / 10 , ((long)options[2].Value % 100) % 10);

            Random random = new();

            Discord.Color clr;
            clr = new((byte)random.Next(0, 255), (byte)random.Next(0, 255), (byte)random.Next(0, 255));

            if (options.FirstOrDefault(x => x.Name == "color") != null)
            {
                Discord.Color tmp_Color;
                try
                {
                    ColorConverter converter = new();
                    System.Drawing.Color tmp_clr = (System.Drawing.Color)converter.ConvertFromString(context: null, culture: CultureInfo.InvariantCulture, text: (string)options.FirstOrDefault(x => x.Name == "color").Value);
                    tmp_Color = new(tmp_clr.R, tmp_clr.G, tmp_clr.B);
                    clr = tmp_Color;
                }
                catch(Exception ex) { Console.WriteLine(ex.Message); }
            }
        


            




            var new_role = await guild.CreateRoleAsync(options[1].Value.ToString(), color: clr);



            guild.GetUser(((SocketGuildUser)options[0].Value).Id).AddRoleAsync(new_role.Id);




            Role role = new(new_role.Id, roleDescription, ((Discord.IGuildUser)options[0].Value), guild.GetUser(command.User.Id), guild);



            await Logging(role);
            await command.RespondAsync("Successfully");
        }


        public async Task Logging(Role role)
        {

            bool serverExists = false;

            XmlDocument xDoc = ErrorProcessing.OpenXDoc("logs.xml");
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
            XmlText userText;
            if (role.user != null) userText = xDoc.CreateTextNode(role.user.Id.ToString());
            else userText = xDoc.CreateTextNode("0");

            XmlElement commanderElem = xDoc.CreateElement("commander");
            XmlText commanderText;
            if (role.commander != null) commanderText = xDoc.CreateTextNode(role.commander.Id.ToString());
            else commanderText = xDoc.CreateTextNode("0");

            XmlElement dateElem = xDoc.CreateElement("date");
            XmlText dateText = xDoc.CreateTextNode(role.date.ToString());

            XmlElement descriptionElem = xDoc.CreateElement("description");
            XmlText descriptionText;
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
                serverText = xDoc.CreateTextNode("off");
                serverAttr = xDoc.CreateAttribute("mode");
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
            Console.WriteLine($"{role.guild.Name} - Role - {role.guild.GetRole(role.id)} - added - users: {role.user} {role.commander}" );

        }

        private async Task HandlerRoleListCommand(SocketSlashCommand command)
        {
            string result = "";
            XmlDocument xDoc = ErrorProcessing.OpenXDoc("logs.xml");
            XmlElement? xRoot = xDoc.DocumentElement;
            var guildId = (ulong)command.GuildId;
            var guild = _client.GetGuild(guildId);

            bool clearMode = false;

            foreach (XmlElement xnode in xRoot)
            {
                var attr = xnode.Attributes.GetNamedItem("id");
                if (ulong.Parse(attr.Value) == guildId)
                {
                    int i = 0;
                    foreach (XmlElement xRole in xnode.ChildNodes)
                    {
                        if (i++ < xnode.ChildNodes.Count - 5) { continue; }
                        if (xnode.Attributes.GetNamedItem("mode").Value == "on") clearMode = true;
                        var roleId = xRole.Attributes.GetNamedItem("id");
                        if (!clearMode) result += "Role: ";
                        if (roleId.Value != "0" && guild.GetRole(ulong.Parse(roleId.Value)) != null) result += guild.GetRole(ulong.Parse(roleId.Value)).Name;
                        else result += "undefined";
                        result += "\n";
                        foreach (XmlNode childNode in xRole.ChildNodes)
                        {
                            if (!clearMode) result += childNode.LocalName;

                            if (!clearMode) result += ": ";
                            if (childNode.Name != "user" && childNode.Name != "commander") result += childNode.InnerText;
                            else
                            {
                                var roleUser = guild.GetUser(ulong.Parse(childNode.InnerText));
                                result += roleUser?.DisplayName ?? roleUser?.Nickname ?? roleUser?.GlobalName ?? roleUser?.Username ?? "Undefined";

                            }
                            result += ' ';
                            result += '\n';
                        }
                        result += '\n';
                    }
                    break;
                }

            }

            var guildUser = (SocketGuildUser)command.User;
            var embedBuiler = new EmbedBuilder()
            .WithAuthor(guildUser.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
            .WithTitle("Last five roles:")
            .WithDescription(result)
            .WithColor(Discord.Color.Blue)
            .WithCurrentTimestamp();





            await command.RespondAsync(embed: embedBuiler.Build());
        }

        private async Task HandlerRoleListFileCommand(SocketSlashCommand command)
        {
            string result = "";
            XmlDocument xDoc = ErrorProcessing.OpenXDoc("logs.xml");
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
                                var roleUser = guild.GetUser(ulong.Parse(childNode.InnerText));
                                result += roleUser?.DisplayName ?? roleUser?.Nickname ?? roleUser?.GlobalName ?? roleUser?.Username ?? "Undefined";
                            }
                            result += ' ';
                            result += '\n';
                        }
                        result += '\n';
                    }
                    break;
                }
            }

            File.WriteAllText("lastRoleHistory.txt", result);

            await command.RespondWithFileAsync("lastRoleHistory.txt");

        }

        private async Task HandlerFindRoleCommand(SocketSlashCommand command)
        {
            string result = "n/a";

            string roleName = "";

            XmlDocument xDoc = ErrorProcessing.OpenXDoc("logs.xml");

            XmlElement? xRoot = xDoc.DocumentElement;

            var guildId = (ulong)command.GuildId;
            var guild = _client.GetGuild(guildId);
            bool clearMode = false;

            foreach (XmlElement xNode in xRoot)
            {
                if (ulong.Parse(xNode.Attributes.GetNamedItem("id").Value) == guildId)
                {
                    if (xNode.Attributes.GetNamedItem("mode").Value == "on") clearMode = true;
                    foreach (XmlNode xRole in xNode)
                    {
                        if (ulong.Parse(xRole.Attributes.GetNamedItem("id").Value) == ((SocketRole)(command.Data.Options.First().Value)).Id)
                        {
                            roleName = guild.GetRole(ulong.Parse(xRole.Attributes.GetNamedItem("id").Value)).Name;
                            result = "";
                            foreach (XmlNode childNode in xRole.ChildNodes)
                            {
                                if (!clearMode)
                                    result += childNode.LocalName;

                                if (!clearMode) result += ": ";
                                if (childNode.Name != "user" && childNode.Name != "commander") result += childNode.InnerText;
                                else
                                {
                                    var roleUser = guild.GetUser(ulong.Parse(childNode.InnerText));
                                    result += roleUser?.DisplayName ?? roleUser?.Nickname ?? roleUser?.GlobalName ?? roleUser?.Username ?? "Undefined";
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
            .WithColor(Discord.Color.Green)
            .WithCurrentTimestamp();





            await command.RespondAsync(embed: embedBuiler.Build());
        }
        private async Task HandlerRoleRemoveCommand(SocketSlashCommand command)
        {
            string err;
            if ((err = PermisionsCheck(command.GuildId, command.User.Id)) != null)
            {
                command.RespondAsync(err);
                return;
            }

            ((SocketRole)command.Data.Options.First().Value).DeleteAsync();
            XmlDocument xDoc = ErrorProcessing.OpenXDoc("logs.xml");

            XmlElement? xRoot = xDoc.DocumentElement;

            foreach (XmlElement xNode in xRoot)
            {
                if (ulong.Parse(xNode.Attributes.GetNamedItem("id").Value) == command.GuildId)
                {
                    foreach (XmlNode xRole in xNode)
                    {
                        if (ulong.Parse(xRole.Attributes.GetNamedItem("id").Value) == ((SocketRole)(command.Data.Options.First().Value)).Id)
                        {
                            xNode.RemoveChild(xRole);
                        }
                    }
                }
            }


            xDoc.Save("logs.xml");
            await command.RespondAsync("Successfully");
        }



        string? PermisionsCheck(ulong? guildId, ulong userId)
        {
            string? result = null;
            SocketGuild guild;
            if (guildId != null)
            {
                if (! _client.GetGuild((ulong)guildId)
                    .CurrentUser
                    .GuildPermissions
                    .ManageRoles) result = "The bot does not have rights to manage roles";
                if (result == null && !_client.GetGuild((ulong)guildId).GetUser(userId).GuildPermissions.ManageRoles)
                {
                    result = "You do not have rights to manage roles";
                }
            }
            return result;
        }
        async Task AddSlashCommandsToGuild(SocketGuild guild) 
        {
            var giveRole = new SlashCommandBuilder();

            var roleList = new SlashCommandBuilder();

            var roleListFile = new SlashCommandBuilder();

            var findRole = new SlashCommandBuilder();

            var roleRemove = new SlashCommandBuilder();

            var clearMode = new SlashCommandBuilder();

            var allRole = new SlashCommandBuilder();

            var roleModify = new SlashCommandBuilder();

            var removeMissRoles = new SlashCommandBuilder();

            roleList.WithName("role-list").WithDescription("Display the role history of this server");
            roleListFile.WithName("role-list-file").WithDescription("Return a txt file with the history of server roles");
            findRole.WithName("find-role").WithDescription("Find the role in the history of this server").AddOption("role", ApplicationCommandOptionType.Role, "name of the role you want to find", isRequired: true);
            roleRemove.WithName("role-remove").WithDescription("Remove a Role from the server and databases").AddOption("role", ApplicationCommandOptionType.Role, "name of the role you want to remove", isRequired: true);
            clearMode.WithName("clear-mode").WithDescription("remove unnecessary text enable/disable");
            allRole.WithName("all-role-add").WithDescription("add existing roles to the database");
            roleModify.WithName("role-modify").WithDescription("Change the description of a role stored in the database").AddOption("role", ApplicationCommandOptionType.Role, "name of the role you want to modify", isRequired: true)
            .AddOption("description", ApplicationCommandOptionType.String, "Description you want to change to", isRequired: true);
            removeMissRoles.WithName("remove-miss-roles").WithDescription("remove missing roles");


            giveRole.WithName("give-role");
            giveRole.WithDescription("issuing a role and recording it in the log");
            giveRole.AddOption("user", ApplicationCommandOptionType.User, "the user to whom you want to add a role", isRequired: true);
            giveRole.AddOption("role_name", ApplicationCommandOptionType.String, "Role name", isRequired: true);
            giveRole.AddOption("description", ApplicationCommandOptionType.String, "Role Description", isRequired: false);
            giveRole.AddOption("color", ApplicationCommandOptionType.String, "Set role color (RGB, HEX and English word)", isRequired: false);


            await guild.CreateApplicationCommandAsync(giveRole.Build());
            await guild.CreateApplicationCommandAsync(roleList.Build());
            await guild.CreateApplicationCommandAsync(roleListFile.Build());
            await guild.CreateApplicationCommandAsync(findRole.Build());
            await guild.CreateApplicationCommandAsync(roleRemove.Build());
            await guild.CreateApplicationCommandAsync(clearMode.Build());
            await guild.CreateApplicationCommandAsync(allRole.Build());
            await guild.CreateApplicationCommandAsync(roleModify.Build());
            await guild.CreateApplicationCommandAsync(removeMissRoles.Build());

        }

    }


    static class ErrorProcessing
    {
        public static XmlDocument? OpenXDoc(string fileName)
        {
            XmlDocument xDoc = new();
            try
            {
                xDoc.Load("logs.xml");
                if (xDoc.DocumentElement.Name != "servers") xDoc = null;
            }
            catch { File.WriteAllText("logs.xml", "<servers>\n</servers>"); xDoc.Load("logs.xml"); }
            return xDoc;
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
        public Role()
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

        public Role(ulong id, string description, IGuildUser user, IGuildUser commander, SocketGuild guild, DateTimeOffset date)
        {
            this.id = id;
            this.description = description;
            this.user = user;
            this.commander = commander;
            this.guild = guild;
            this.date = date.DateTime;
        }

    }

}