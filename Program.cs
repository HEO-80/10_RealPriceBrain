using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;

class Program
{
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

    static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(" 📡 RADAR V3 - ESCÁNER MULTI-DEX (PANCAKE vs BISWAP) ");
        Console.WriteLine("--------------------------------------------------\n");
        Console.ResetColor();

        string rpcUrl = "https://bnb-mainnet.g.alchemy.com/v2/aNChN_RMYlL4TN5pbzVEK";
        var web3 = new Web3(rpcUrl);

        // Direcciones oficiales de los Pools WBNB/USDT
        string poolPancake = "0x16b9a82891338f9bA80E2D6970FddA79D1eb0daE";
        string poolBiswap = "0xa98ea6356A316b44Bf710D8f9b6b4eA0081409EF"; 

        var getReservesMessage = new GetReservesFunction();
        var handler = web3.Eth.GetContractQueryHandler<GetReservesFunction>();

        Console.WriteLine("Ojos puestos en PancakeSwap y Biswap...");
        Console.WriteLine("Buscando oportunidades de arbitraje...\n");

        while (true)
        {
            try
            {
                // 1. Consultamos AMBOS exchanges a la vez
                var resPancake = await handler.QueryDeserializingToObjectAsync<ReservesOutput>(getReservesMessage, poolPancake);
                var resBiswap = await handler.QueryDeserializingToObjectAsync<ReservesOutput>(getReservesMessage, poolBiswap);

                // Solo seguimos si ambos responden correctamente
                if (resPancake != null && resBiswap != null && resPancake.Reserve0 > 0 && resBiswap.Reserve0 > 0)
                {
                    // 2. Calculamos Precio en PancakeSwap (USDT es Token0, WBNB es Token1)
                    decimal pUsdt0 = (decimal)resPancake.Reserve0 / 1000000000000000000m;
                    decimal pWbnb1 = (decimal)resPancake.Reserve1 / 1000000000000000000m;
                    decimal pricePancake = pUsdt0 / pWbnb1;

                    // 3. Calculamos Precio en Biswap (Igual: USDT es Token0, WBNB es Token1)
                    decimal bUsdt0 = (decimal)resBiswap.Reserve0 / 1000000000000000000m;
                    decimal bWbnb1 = (decimal)resBiswap.Reserve1 / 1000000000000000000m;
                    decimal priceBiswap = bUsdt0 / bWbnb1;

                    // 4. Calculamos la diferencia bruta (Spread)
                    decimal spread = Math.Abs(pricePancake - priceBiswap);

                    // Imprimimos la comparativa en pantalla
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Pancake: {pricePancake:F2} | Biswap: {priceBiswap:F2} | Spread: {spread:F3} USDT");
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