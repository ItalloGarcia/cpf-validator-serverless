using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace CPFValidation
{
    public static class CPFValidatorFunction
    {
        [FunctionName("ValidateCPF")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CPF validation request received.");

            try
            {
                // Read and parse the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                // Validate input
                if (data == null || string.IsNullOrEmpty(data?.cpf?.ToString()))
                {
                    return new BadRequestObjectResult("CPF is required.");
                }

                string cpf = data.cpf.ToString();

                // Validate CPF format and digits
                if (!IsValidCPF(cpf))
                {
                    return new BadRequestObjectResult("Invalid CPF.");
                }

                return new OkObjectResult("Valid CPF.");
            }
            catch (Exception ex)
            {
                log.LogError($"An error occurred: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private static bool IsValidCPF(string cpf)
        {
            // Remove non-numeric characters
            cpf = Regex.Replace(cpf, @"[^\d]", "");

            // Basic validation
            if (cpf.Length != 11 || !long.TryParse(cpf, out _))
            {
                return false;
            }

            // Check for repeated digits or known invalid CPFs
            if (IsRepeatedDigits(cpf) || cpf == "12345678909")
            {
                return false;
            }

            // Calculate and validate check digits
            int[] firstMultiplier = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] secondMultiplier = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cpf.Substring(0, 9);
            int sum = CalculateSum(tempCpf, firstMultiplier);
            int remainder = CalculateRemainder(sum);

            string digit = remainder.ToString();
            tempCpf += digit;

            sum = CalculateSum(tempCpf, secondMultiplier);
            remainder = CalculateRemainder(sum);

            digit += remainder.ToString();

            return cpf.EndsWith(digit);
        }

        private static bool IsRepeatedDigits(string cpf)
        {
            for (int i = 1; i < cpf.Length; i++)
            {
                if (cpf[i] != cpf[0])
                {
                    return false;
                }
            }
            return true;
        }

        private static int CalculateSum(string input, int[] multipliers)
        {
            int sum = 0;
            for (int i = 0; i < multipliers.Length; i++)
            {
                sum += int.Parse(input[i].ToString()) * multipliers[i];
            }
            return sum;
        }

        private static int CalculateRemainder(int sum)
        {
            int remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }
    }
}
