using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;

class Program
{
    // 1. NUEVO: Definimos la petición a la blockchain como una clase exacta
    [Function("getReserves")]
    public class GetReservesFunction : FunctionMessage
    {
    }

    // 2. Definimos la respuesta de la blockchain
    [FunctionOutput]
    public class ReservesOutput : IFunctionOutputDTO
    {
        [Parameter("uint112", "_reserve0", 1)]
        public BigInteger Reserve0 { get; set; }

        [Parameter("uint112", "_reserve1", 2)]
        public BigInteger Reserve1 { get; set; }

        [Parameter("uint32", "_blockTimestampLast", 3)]
        public uint BlockTimestampLast { get; set; }
    }

    static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(" 📡 RADAR V2 - LEYENDO PRECIOS EN PANCAKESWAP     ");
        Console.WriteLine("--------------------------------------------------\n");
        Console.ResetColor();

        string rpcUrl = "https://bnb-mainnet.g.alchemy.com/v2/aNChN_RMYlL4TN5pbzVEK";
        var web3 = new Web3(rpcUrl);

        string poolAddress = "0x16b9a82891338f9bA80E2D69CdDdFd8E10103A4E";

        // 3. NUEVO: Usamos un "Handler" tipado en lugar del string ABI
        var getReservesHandler = web3.Eth.GetContractQueryHandler<GetReservesFunction>();
        var functionMessage = new GetReservesFunction();

        Console.WriteLine($"Ojos puestos en el Pool: {poolAddress}");
        Console.WriteLine("Iniciando escáner...\n");

        while (true)
        {
            try
            {
                // Hacemos la llamada usando el nuevo sistema seguro
                var reserves = await getReservesHandler.QueryDeserializingToObjectAsync<ReservesOutput>(functionMessage, poolAddress);
                // Seguro extra: comprobamos si realmente vienen vacías
                if (reserves.Reserve0 == 0) 
                {
                    Console.WriteLine("La red ha devuelto 0 reservas. Reintentando...");
                }
                else
                {
                    // Convertimos Wei a decimales normales
                    decimal reserve0WBNB = (decimal)reserves.Reserve0 / 1000000000000000000m;
                    decimal reserve1USDT = (decimal)reserves.Reserve1 / 1000000000000000000m;

                    // Calculamos precio
                    decimal price = reserve1USDT / reserve0WBNB;

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 1 WBNB = {price:F2} USDT");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error de lectura: {e.Message}");
            }

            await Task.Delay(3000);
        }
    }
}