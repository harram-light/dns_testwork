using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace FactorialServer.Services.Abstractions
{
    public interface IFactorialCalculator
    {
        Task<string> CalculateAsync(long number);
    }
}