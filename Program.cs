using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using DotNetEnv;

// 1. Clases de Nethereum para interactuar con los Smart Contracts
[Function("getReserves", typeof(ReservesOutput))]
public class GetReservesFunction : FunctionMessage { }

[FunctionOutput]
public class ReservesOutput : IFunctionOutputDTO
{
    [Parameter("uint112", "_reserve0", 1)]
    public BigInteger Reserve0 { get; set; }

    [Parameter("uint112", "_reserve1", 2)]
    public BigInteger Reserve1 { get; set; }

    [Parameter("uint32", "_blockTimestampLast", 3)]
    public BigInteger BlockTimestampLast { get; set; } 
}

class Program
{
    // UN SOLO MÉTODO MAIN
    static async Task Main(string[] args)
    {
        // 2. Abrimos el sobre secreto (.env)
        Env.Load();

        // Extraemos las coordenadas y las llaves
        string rpcUrl = Environment.GetEnvironmentVariable("ALCHEMY_RPC_URL");
        string privateKey = Environment.GetEnvironmentVariable("PRIVATE_KEY");
        string botAddress = Environment.GetEnvironmentVariable("FLASHBOT_ADDRESS");

        // 3. Interfaz de Consola
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(" 📡 RADAR V3 - ESCÁNER MULTI-DEX (PANCAKE vs BISWAP) ");
        Console.WriteLine("--------------------------------------------------\n");
        Console.ResetColor();

        Console.WriteLine($"[+] Vinculando Cerebro con el Bot en: {botAddress}");
        Console.WriteLine($"[+] Conectando al nodo RPC...\n");

        // 4. Inicializamos Web3 usando la URL que viene del .env
        var web3 = new Web3(rpcUrl);

        // Direcciones oficiales de los Pools
        string poolPancake = "0x16b9a82891338f9bA80E2D6970FddA79D1eb0daE";
        string poolB = "0xd99c7F6C65857AC913a8f880A4cb84032AB2FC5b"; 

        var getReservesMessage = new GetReservesFunction();
        var handler = web3.Eth.GetContractQueryHandler<GetReservesFunction>();

        Console.WriteLine("Ojos puestos en PancakeSwap y el Pool B...");
        Console.WriteLine("Buscando oportunidades de arbitraje...\n");

        // 5. Bucle infinito del Radar
        while (true)
        {
            try
            {
                var resPancake = await handler.QueryDeserializingToObjectAsync<ReservesOutput>(getReservesMessage, poolPancake);
                var resExchangeB = await handler.QueryDeserializingToObjectAsync<ReservesOutput>(getReservesMessage, poolB);

                if (resPancake != null && resExchangeB != null && resPancake.Reserve0 > 0 && resExchangeB.Reserve0 > 0)
                {
                    decimal pUsdt0 = (decimal)resPancake.Reserve0 / 1000000000000000000m;
                    decimal pWbnb1 = (decimal)resPancake.Reserve1 / 1000000000000000000m;
                    decimal priceA = pUsdt0 / pWbnb1;

                    decimal bUsdc0 = (decimal)resExchangeB.Reserve0 / 1000000000000000000m;
                    decimal bWbnb1 = (decimal)resExchangeB.Reserve1 / 1000000000000000000m;
                    decimal priceB = bUsdc0 / bWbnb1;

                    decimal spread = Math.Abs(priceA - priceB);

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] USDT-BNB: {priceA:F2} | USDC-BNB: {priceB:F2} | Spread: {spread:F3} $");
                }
                else 
                {
                    Console.WriteLine("Aviso: Uno de los pools está vacío o la dirección es incorrecta.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Excepción atrapada: {e.Message}");
            }

            await Task.Delay(3000); 
        }
    }
}