using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts; // NUEVO: Para firmar transacciones
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using DotNetEnv;

// --- CLASES DE LECTURA ---
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

// --- CLASE DE ESCRITURA (EL GATILLO) ---
[Function("iniciarArbitraje")]
public class IniciarArbitrajeFunction : FunctionMessage
{
    [Parameter("address", "_poolPrestamo", 1)]
    public string PoolPrestamo { get; set; }

    [Parameter("uint256", "_cantidadUSDT", 2)]
    public BigInteger CantidadUSDT { get; set; }

    [Parameter("address", "_routerCompra", 3)]
    public string RouterCompra { get; set; }

    [Parameter("address", "_routerVenta", 4)]
    public string RouterVenta { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        Env.Load();
        string rpcUrl = Environment.GetEnvironmentVariable("ALCHEMY_RPC_URL");
        string privateKey = Environment.GetEnvironmentVariable("PRIVATE_KEY");
        string botAddress = Environment.GetEnvironmentVariable("FLASHBOT_ADDRESS");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(" 📡 RADAR V3 - ESCÁNER MULTI-DEX (PANCAKE vs BISWAP) ");
        Console.WriteLine("--------------------------------------------------\n");
        Console.ResetColor();

        Console.WriteLine($"[+] Vinculando Cerebro con el Bot en: {botAddress}");
        
        // NUEVO: Instanciamos la cuenta para poder FIRMAR transacciones
        var account = new Account(privateKey);
        var web3 = new Web3(account, rpcUrl); 
        
        Console.WriteLine($"[+] Cuenta de ataque autorizada: {account.Address}\n");

        string poolPancake = "0x16b9a82891338f9bA80E2D6970FddA79D1eb0daE";
        string poolB = "0xd99c7F6C65857AC913a8f880A4cb84032AB2FC5b"; 

        var getReservesMessage = new GetReservesFunction();
        var handler = web3.Eth.GetContractQueryHandler<GetReservesFunction>();

        Console.WriteLine("Ojos puestos en el mercado...");
        Console.WriteLine("Buscando oportunidades de arbitraje...\n");

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

                    // --- LA REGLA DE DISPARO ---
                    decimal umbralMinimo = 10.0m; // Dispara si hay 10 dolares de diferencia

                    if (spread >= umbralMinimo)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n[!!!] OPORTUNIDAD DETECTADA: Spread de {spread:F3} $");
                        Console.WriteLine("[!!!] INICIANDO SECUENCIA DE ARBITRAJE...");
                        Console.ResetColor();

                        var arbitrajeMessage = new IniciarArbitrajeFunction()
                        {
                            PoolPrestamo = poolPancake,
                            CantidadUSDT = Web3.Convert.ToWei(100), // Pedimos prestados 100 USDT para probar
                            RouterCompra = "0x10ED43C718714eb63d5aA57B78B54704E256024E", // Router Pancake
                            RouterVenta = "0x3a6d8cA21D1CF76F653A67577FA0D27453350dD8", // Router Biswap
                            Gas = 500000 // Limite de seguridad de gas
                        };

                        var txHandler = web3.Eth.GetContractTransactionHandler<IniciarArbitrajeFunction>();
                        string txHash = await txHandler.SendRequestAsync(botAddress, arbitrajeMessage);

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[+] ¡DISPARO REALIZADO! Hash: {txHash}");
                        Console.WriteLine($"[+] Pausando radar 30 segundos para enfriar armas...\n");
                        Console.ResetColor();
                        
                        await Task.Delay(30000); // Enfriamiento de 30 segundos
                    }
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