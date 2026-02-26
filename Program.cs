using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;

class Program
{
    // 1. Definimos la estructura exacta de datos que nos devuelve PancakeSwap
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

        // 2. Conexión a la red principal de Binance
        string rpcUrl = "https://bnb-mainnet.g.alchemy.com/v2/aNChN_RMYlL4TN5pbzVEK";
        var web3 = new Web3(rpcUrl);

        // 3. Dirección del Pool WBNB/USDT en PancakeSwap V2
        string poolAddress = "0x16b9a82891338f9bA80E2D69CdDdFd8E10103A4E";

        // ABI mínima para poder llamar a la función getReserves
        string abi = @"[{'constant':true,'inputs':[],'name':'getReserves','outputs':[{'internalType':'uint112','name':'_reserve0','type':'uint112'},{'internalType':'uint112','name':'_reserve1','type':'uint112'},{'internalType':'uint32','name':'_blockTimestampLast','type':'uint32'}],'payable':false,'stateMutability':'view','type':'function'}]";

        var contract = web3.Eth.GetContract(abi, poolAddress);
        var getReservesFunction = contract.GetFunction("getReserves");

        Console.WriteLine($"Ojos puestos en el Pool: {poolAddress}");
        Console.WriteLine("Iniciando escáner...\n");

        // 4. Bucle infinito para leer el precio cada 3 segundos
        while (true)
        {
            try
            {
                // Hacemos la llamada gratuita de lectura
                var reserves = await getReservesFunction.CallDeserializingToObjectAsync<ReservesOutput>();

                // Convertimos el formato Wei (18 ceros) a decimal matemático normal
                decimal reserve0WBNB = (decimal)reserves.Reserve0 / 1000000000000000000m;
                decimal reserve1USDT = (decimal)reserves.Reserve1 / 1000000000000000000m;

                // Calculamos el precio dividiendo la reserva de USDT entre la de WBNB
                decimal price = reserve1USDT / reserve0WBNB;

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 1 WBNB = {price:F2} USDT");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error de lectura: {e.Message}");
            }

            await Task.Delay(3000); // Esperamos 3 segundos para no saturar Alchemy
        }
    }
}