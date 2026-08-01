// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;
using System.Security.Cryptography;

namespace NG.Velox.Benchmarks
{
    [CPUUsageDiagnoser]
    public class Benchmarks
    {
        private SHA256 sha256 = SHA256.Create();
        private byte[] data;

        [GlobalSetup]
        public void Setup()
        {
            data = new byte[10000];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256()
        {
            return sha256.ComputeHash(data);
        }
    }
}
