# Yuno Bot for Discord

## Usage

### Current Commands

| Group | Command | Description | Max Params |
| :-----: | ------- | ----------- | ---------- |
| General | hello | Say hello! | - |
| General | hello &lt;message> | Echos message | &#8734; |
| General | help | Displays help | - |
| General | help &lt;group> | Displays help for a specific group | 1 | 
| search | rank &lt;names> | Searches for Summoners and retrieves their solo queue and flex queue ranks | 5 |
| search | winrate &lt;names> | Returns the Win/Loss ratio for the last 20 Ranked games (solo or flex) of a player | 1 |
| search | ingame &lt;name> | Returns a list of summoner ranks from the specified players active game | 1 |
| admin | list | List important people (defined as those with certain permissions) in a Discord server | - |
| debug | updateLeague | Update the league patch number remotely | - |
| debug | allServices | return a list of running services in the collection | - |
| debug | halt | Stop the running bot |

For example: <b>`search rank "Yandere Supreme"</b> results in my ranked information being returned. (Im hardstuck Gold IV mid pls help me)

Some commands can have multiple parameters: <b>`search rank "Yandere Supreme" "TSM Bjergsen"</b>.

Commands without a group can be invoked directly: <b> `hello</b>

### Config.json

  Generally these are pretty self explanatory, but I will still list them here for clarity in
  editing the config file. Right now there are very few options, but as I continue to refine
  the bot more options will be made available. While some are not required, they will always
  use the default value if there is one as realistically they are values needed to function.

| param     | value explanation                     | Required | Default    |
| ------- | ----------------------------------- | :------: | :--------: |
| riotKey   | Official Riot API key | Yes |
| botToken  | Discord Bot Token | Yes |
| clientSeceret | Discord Client Seceret ( so far unused) | No |
| clientId | Discord Client Id Number, this is the bot's user ID number visible to anyone | No |
| prefix | The prefix a user should type to trigger a command (ie the '!' in "!hello") | No | ` |
| logLevel | The Logging Level tracked to standard output| No | 3(info) |
| matchCache | File path to store the match cache after after exiting. Relative or direct. | No | matchCache.lol |

### Log Level

| Name | Value |
| ------- | :---: |
| Critical | 0 |
| Error | 1 |
| Warning | 2 |
| Info | 3 |
| Verbose | 4 |
| Debug | 5 |

 Logging is done by anything below the log level set. For example, setting logging to 3 means info, warnings, errors, and
 critical failures will all be output. It is not reccommended to set logging below Warning. Verbose exposes messages that
 trigger a command and the information on the output of the command (NOT YET IMPLEMENTED), and Debug will expose the
 entire process behind building the output (NOT YET IMPLEMENTED).

## Layout/Design

 The design of this bot primarily utilizes the Discord.NET library for handling Discord related functionality and the
 Camille library for accessing Riot Games' League of Legends data web API. Discord.NET's bot command framework is heavily
 influenced by ASP.NET Core's controller pattern, with static services and then modules being loaded and unloaded as
 commands are invoked. [It can be read more about here]("https://discord.foxbot.me/docs/guides/commands/intro.html").
 The services provide information and data to the modules which process and send the information back the user. The
 entire bot is written to be asynchronous so that many users can be simultaneously utilizing the bot without diminishing
 performance.
 
 The first service is the discord command service. This service orchestrates the creation of commands by users on Discord
 interacting with the bot. It detects when a message with the desired prefix and then executes the command by creating
 the module that contains the command. When the module is finished executing the command, it deconstructs.

 The second service, called RapiInfo within the source, is primarily concerned with storing information about the current
 state of League of Legends, as well as containing the Camille.RiotApi object to fetch information from the Riot servers.

## Purpose

 I started this project as a fun way to learn asynchronous programming and C#. I am already quite familiar with C/C++, so
 I felt like a natural step would be to learn C# with its more modern features and support. While C# does abstract out
 most of the finer deatails of asynchronous programming, it was quite hard to wrap my brain around the exact nature of
 async. Once I straightened out how exactly async can be used to execute many tasks in parralel, it became quite clear
 that the benefits are enormous for a web based application such as this, allowing computing to be done while waiting on
 information to be recieved and parsed from a remote service.

### contact
 galindez@bu.edu