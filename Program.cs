using System;
using System.Threading.Tasks;
using BaseballCli.Commands;

namespace BaseballCli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var app = new BaseballCliApp();
            return await app.RunAsync(args);
        }
    }
}
