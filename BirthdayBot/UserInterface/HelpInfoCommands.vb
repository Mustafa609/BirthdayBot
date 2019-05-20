﻿Imports System.Text
Imports Discord
Imports Discord.WebSocket

Friend Class HelpInfoCommands
    Inherits CommandsCommon

    Private ReadOnly _helpEmbed As Embed
    Private ReadOnly _helpConfigEmbed As Embed
    Private ReadOnly _discordClient As DiscordSocketClient

    Sub New(inst As BirthdayBot, db As Configuration, client As DiscordSocketClient)
        MyBase.New(inst, db)
        _discordClient = client
        Dim embeds = BuildHelpEmbeds()
        _helpEmbed = embeds.Item1
        _helpConfigEmbed = embeds.Item2
    End Sub

    Public Overrides ReadOnly Property Commands As IEnumerable(Of (String, CommandHandler))
        Get
            Return New List(Of (String, CommandHandler)) From {
                ("help", AddressOf CmdHelp),
                ("help-config", AddressOf CmdHelpConfig),
                ("help-tzdata", AddressOf CmdHelpTzdata),
                ("info", AddressOf CmdInfo)
            }
        End Get
    End Property

    Private Function BuildHelpEmbeds() As (Embed, Embed)
        Dim cpfx = $"●`{CommandPrefix}"
        ' Normal section
        Dim cmdField As New EmbedFieldBuilder With {
            .Name = "Commands",
            .Value =
                $"{cpfx}help`, `{CommandPrefix}info`, `{CommandPrefix}help-tzdata`" + vbLf +
                $" » Help and informational messages." + vbLf +
                $"{cpfx}set (date) [zone]`" + vbLf +
                $" » Registers your birth date. Time zone is optional." + vbLf +
                $" »» Examples: `{CommandPrefix}set jan-31`, `{CommandPrefix}set 15-aug America/Los_Angeles`." + vbLf +
                $"{cpfx}zone (zone)`" + vbLf +
                $" » Sets your local time zone. See `{CommandPrefix}help-tzdata`." + vbLf +
                $"{cpfx}remove`" + vbLf +
                $" » Removes your birthday information from this bot." + vbLf +
                $"{cpfx}config`" + vbLf +
                $" » Edit bot configuration. Moderators only. See `{CommandPrefix}help-config`." + vbLf +
                $"{cpfx}override (user ping or ID) (command w/ parameters)`" + vbLf +
                " » Perform certain commands on behalf of another user. Moderators only."
        }
        Dim helpRegular As New EmbedBuilder
        helpRegular.AddField(cmdField)

        ' Manager section
        Dim mpfx = cpfx + "config "
        Dim configField1 As New EmbedFieldBuilder With {
            .Name = "Basic settings",
            .Value =
                $"{mpfx}role (role name or ID)`" + vbLf +
                " » Sets the role to apply to users having birthdays." + vbLf +
                $"{mpfx}channel (channel name or ID)`" + vbLf +
                " » Sets the announcement channel. Leave blank to disable." + vbLf +
                $"{mpfx}message (message)`" + vbLf +
                " » Sets a custom announcement message. Use `%n` to specify where the name(s) should be displayed." + vbLf +
                $"{mpfx}messagepl (message)`" + vbLf +
                " » ""Message Plural"". Sets the message used when two or more people are on the birthday list." + vbLf +
                " » `%n` may be used here as well. It is highly recommended to specify a singular and a plural message." + vbLf +
                $"{mpfx}zone (time zone name)`" + vbLf +
                " » Sets the default server time zone. See `{CommandPrefix}help-tzdata`."
        }
        Dim configField2 As New EmbedFieldBuilder With {
            .Name = "Access management",
            .Value =
                $"{mpfx}modrole (role name, role ping, or ID)`" + vbLf +
                " » Establishes a role for bot moderators. Grants access to `bb.config` and `bb.override`." + vbLf +
                $"{mpfx}block/unblock (user ping or ID)`" + vbLf +
                " » Prevents or allows usage of bot commands to the given user." + vbLf +
                $"{mpfx}moderated on/off`" + vbLf +
                " » Prevents or allows using commands for all members excluding moderators."
        }

        Dim helpConfig As New EmbedBuilder
        helpConfig.Author = New EmbedAuthorBuilder() With {.Name = $"{CommandPrefix}config subcommands"}
        helpConfig.Description = "All the following subcommands are only usable by moderators and server managers."
        helpConfig.AddField(configField1)
        helpConfig.AddField(configField2)

        Return (helpRegular.Build(), helpConfig.Build())
    End Function

    Private Async Function CmdHelp(param As String(), reqChannel As SocketTextChannel, reqUser As SocketGuildUser) As Task
        Await reqChannel.SendMessageAsync(embed:=_helpEmbed)
    End Function

    Private Async Function CmdHelpConfig(param As String(), reqChannel As SocketTextChannel, reqUser As SocketGuildUser) As Task
        Await reqChannel.SendMessageAsync(embed:=_helpConfigEmbed)
    End Function

    Private Async Function CmdHelpTzdata(param As String(), reqChannel As SocketTextChannel, reqUser As SocketGuildUser) As Task
        Const tzhelp = "You may specify a time zone in order to have your birthday recognized with respect to your local time. " +
            "This bot only accepts zone names from the IANA Time Zone Database (a.k.a. Olson Database)." + vbLf + vbLf +
            "These names can be found at the following link, under the 'TZ database name' column: " +
            "https://en.wikipedia.org/wiki/List_of_tz_database_time_zones"
        Dim embed As New EmbedBuilder
        embed.AddField(New EmbedFieldBuilder() With {
            .Name = "Time Zone Support",
            .Value = tzhelp
        })
        Await reqChannel.SendMessageAsync(embed:=embed.Build())
    End Function

    Private Async Function CmdInfo(param As String(), reqChannel As SocketTextChannel, reqUser As SocketGuildUser) As Task
        ' Bot status field
        Dim strStatus As New StringBuilder
        Dim asmnm = Reflection.Assembly.GetExecutingAssembly.GetName()
        strStatus.AppendLine("Birthday Bot version " + asmnm.Version.ToString(3))
        strStatus.AppendLine("Server count: " + _discordClient.Guilds.Count.ToString())
        strStatus.AppendLine("Uptime: " + (DateTimeOffset.UtcNow - Program.BotStartTime).ToString("d' days, 'hh':'mm':'ss"))
        strStatus.Append("More info will be shown here soon.")

        ' TODO fun stats
        ' current birthdays, total names registered, unique time zones

        Dim embed As New EmbedBuilder With {
            .Author = New EmbedAuthorBuilder() With {
                .Name = "Thank you for using Birthday Bot!",
                .IconUrl = _discordClient.CurrentUser.GetAvatarUrl()
            },
            .Description = "Suggestions and feedback are always welcome. Please refer to the listing on Discord Bots " +
            "(discord.bots.gg) for information on reaching my personal server. I may not be available often, but I am happy to " +
            "respond to feedback in due time." + vbLf +
            "This bot remains very much in its early stages. Essential and quality-of-life features will be slowly added over time."
        }
        Dim verstr = Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3)
        embed.AddField(New EmbedFieldBuilder With {
            .Name = "Statistics",
            .Value = strStatus.ToString()
        })
        Await reqChannel.SendMessageAsync(embed:=embed.Build())
    End Function
End Class
