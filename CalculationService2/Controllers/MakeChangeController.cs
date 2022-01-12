using Microsoft.AspNetCore.Mvc;

namespace CalculationService2.Controllers;

[Route("ces2/[controller]")]
[ApiController]
public class MakeChangeController : ControllerBase
{
    [HttpGet("{amount}")]
    public async Task<string> Get(int amount)
    {
        var result = await Task.Run(() =>
        {
            var runningAmount = amount;

            var quarters = runningAmount / 25;

            runningAmount -= (quarters * 25);

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