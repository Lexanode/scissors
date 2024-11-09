using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordIS.Modules;

public class EchoModule : ModuleBase<SocketCommandContext>
{
    [Command("echo")]
    public async Task EchoAsync([Remainder] string str)
    {
        await ReplyAsync(str);
    }
}