using Microsoft.AspNetCore.Mvc;

namespace CalculationService2.Controllers;

[Route("ces2/[controller]")]
[ApiController]
public class MakeChangeController : ControllerBase
{
    private int _primeNumber;


    public MakeChangeController(IConfiguration configuration)
    {
        _primeNumber = configuration.GetValue<int>("PrimeNumber");
    }

    private long FindPrimeNumber(int n)
    {
        if (_primeNumber == -1) return _primeNumber;

        var count = 0;
        long a = 2;

        while (count < n)
        {
            long b = 2;
            var prime = 1;// to check if found a prime

            while (b * b <= a)
            {
                if (a % b == 0)
                {
                    prime = 0;

                    break;
                }

                b++;
            }

            if (prime > 0)
            {
                count++;
            }

            a++;
        }

        return --a;
    }

    [HttpGet("{amount}")]
    public async Task<string> Get(int amount)
    {
        var result = await Task.Run(() =>
        {
            var nthPrime = FindPrimeNumber(_primeNumber);
            
            var runningAmount = amount;

            var quarters = runningAmount / 25;

            runningAmount -= quarters * 25;

            var dimes = runningAmount / 10;

            runningAmount -= dimes * 10;

            var nickels = runningAmount / 5;

            runningAmount -= nickels * 5;

            var pennies = runningAmount;

            return $"{quarters}Q, {dimes}D, {nickels}N, {pennies}P";
        });

        return result;
    }
}