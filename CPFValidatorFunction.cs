using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CPFValidation
{
    public static class CPFValidatorFunction
    {
        [FunctionName("ValidateCPF")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Starting CPF validation.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            if (data == null)
            {
                return new BadRequestObjectResult("Please provide a CPF.");
            }

            string cpf = data?.cpf;

            if (IsValidCPF(cpf))
            {
                return new OkObjectResult("Valid CPF.");
            }
            else
            {
                return new BadRequestObjectResult("Invalid CPF.");
            }
        }

        public static bool IsValidCPF(string cpf)
        {
            if (string.IsNullOrEmpty(cpf))
                return false;

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (cpf.Length != 11)
                return false;

            // Check if all digits are the same
            bool allDigitsSame = true;
            for (int i = 1; i < 11 && allDigitsSame; i++)
            {
                if (cpf[i] != cpf[0])
                    allDigitsSame = false;
            }

            if (allDigitsSame || cpf == "12345678909")
                return false;

            int[] firstMultiplier = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] secondMultiplier = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cpf.Substring(0, 9);
            int sum = 0;

            for (int i = 0; i < 9; i++)
            {
                sum += int.Parse(tempCpf[i].ToString()) * firstMultiplier[i];
            }

            int remainder = sum % 11;
            remainder = remainder < 2 ? 0 : 11 - remainder;

            string digit = remainder.ToString();
            tempCpf = tempCpf + digit;
            sum = 0;

            for (int i = 0; i < 10; i++)
            {
                sum += int.Parse(tempCpf[i].ToString()) * secondMultiplier[i];
            }

            remainder = sum % 11;
            remainder = remainder < 2 ? 0 : 11 - remainder;

            digit = digit + remainder.ToString();

            return cpf.EndsWith(digit);
        }
    }
}