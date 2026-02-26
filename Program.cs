using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;

class Program
{
    // 1. Vinculamos explícitamente la clase de salida para ayudar al traductor
    [Function("getReserves", typeof(ReservesOutput))]
    public class GetReservesFunction : FunctionMessage
    {
    }

    [FunctionOutput]
    public class ReservesOutput : IFunctionOutputDTO
    {
        [Parameter("uint112", "_reserve0", 1)]
        public BigInteger Reserve0 { get; set; }

        [Parameter("uint112", "_reserve1", 2)]
        public BigInteger Reserve1 { get; set; }

        // 2. Usamos BigInteger para absorber cualquier formato numérico sin que C# explote
        [Parameter("uint32", "_blockTimestampLast", 3)]
        public BigInteger BlockTimestampLast { get; set; } 
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

        var getReservesMessage = new GetReservesFunction();
        var handler = web3.Eth.GetContractQueryHandler<GetReservesFunction>();

        Console.WriteLine($"Ojos puestos en el Pool: {poolAddress}");
        Console.WriteLine("Iniciando escáner...\n");

        while (true)
        {
            try
            {
                // Hacemos la llamada a la red
                var reserves = await handler.QueryDeserializingToObjectAsync<ReservesOutput>(getReservesMessage, poolAddress);

                // 3. NUEVO: Red de seguridad. Si viene nulo o vacío, no intentamos leerlo.
                if (reserves == null)
                {
                    Console.WriteLine("Aviso: La red devolvió un valor nulo. Comprobando...");
                }
                else if (reserves.Reserve0 == 0)
                {
                    Console.WriteLine("Aviso: El pool indica que tiene 0 reservas (Vacío).");
                }
                else
                {
                    // Convertimos el estándar Wei a decimales puros
                    decimal reserve0WBNB = (decimal)reserves.Reserve0 / 1000000000000000000m;
                    decimal reserve1USDT = (decimal)reserves.Reserve1 / 1000000000000000000m;

                    // Matemáticas simples: Precio = USDT / WBNB
                    decimal price = reserve1USDT / reserve0WBNB;

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 1 WBNB = {price:F2} USDT");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Excepción atrapada: {e.Message}");
            }

            await Task.Delay(3000); // Respiro de 3 segundos para no bloquear la API
        }
    }
}