﻿using ccminer_gui.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ccminer_gui
{
    class CCMinerInterop
    {
        public event DataReceivedEventHandler OutputDataReceived;
        public event DataReceivedEventHandler ErrorDataReceived;

        public CCMinerInterop()
        {
            _minerCli = new MinerCli();

            _minerCli.OutputDataReceived += _minerCli_OutputDataReceived;
            _minerCli.ErrorDataReceived += _minerCli_ErrorDataReceived;

            _algorithms = new List<Algorithm>()
            {
                /*Algorithm.Create("bastion", "Joincoin"),
                Algorithm.Create("bitcore", "Bitcore's Timetravel10"),
                Algorithm.Create("blake", "Saffroncoin(Blake256)"),
                Algorithm.Create("blakecoin", "Old Blake 256"),
                Algorithm.Create("blake2s", "Nevacoin(Blake2-S 256)"),
                Algorithm.Create("bmw", "Midnight"),
                Algorithm.Create("cryptolight", "AEON cryptonight(MEM/2)"),
                Algorithm.Create("cryptonight", "XMR cryptonight"),
                Algorithm.Create("c11/flax", "Chaincoin and Flax"),
                Algorithm.Create("decred", "Decred 180 bytes Blake256-14"),
                Algorithm.Create("deep", "Deepcoin"),
                Algorithm.Create("dmd-gr", "Diamond-Groestl"),
                Algorithm.Create("equihash", "ZEC, HUSH and KMD"),
                Algorithm.Create("fresh", "Freshcoin"),
                Algorithm.Create("fugue256", "Fuguecoin"),
                Algorithm.Create("groestl", "Groestlcoin"),
                Algorithm.Create("heavy", "Heavycoin"),
                Algorithm.Create("hsr", "Hshare"),
                Algorithm.Create("jackpot", "Sweepcoin"),
                Algorithm.Create("keccak", "Maxcoin"),
                Algorithm.Create("keccakc", "CreativeCoin"),
                Algorithm.Create("lbry", "LBRY Credits"),
                Algorithm.Create("luffa", "Joincoin"),
                Algorithm.Create("lyra2", "CryptoCoin"),
                Algorithm.Create("lyra2v2", "Vertcoin"),
                Algorithm.Create("lyra2z", "Zerocoin(XZC)"),
                Algorithm.Create("mjollnir", "Mjollnircoin"),
                Algorithm.Create("myr-gr", "Myriad-Groest"),
                Algorithm.Create("neoscrypt", "FeatherCoin"),
                Algorithm.Create("nist5", "TalkCoin"),
                Algorithm.Create("penta", "Joincoin / Pentablake"),
                Algorithm.Create("phi", "LUXCoin"),
                Algorithm.Create("polytimos", "Polytimos"),
                Algorithm.Create("quark", "Quarkcoin"),
                Algorithm.Create("qubit", "Qubit"),
                Algorithm.Create("scrypt", "Scrypt coins"),
                Algorithm.Create("scrypt:N", "Scrypt-N(:10 for 2048 iterations)"),
                Algorithm.Create("scrypt-jane", "Chacha coins like Cache and Ultracoin"),
                Algorithm.Create("s3", "1coin(ONE)"),
                Algorithm.Create("sha256t", "OneCoin(OC)"),
                Algorithm.Create("sia", "SIA"),
                Algorithm.Create("sib", "Sibcoin"),
                Algorithm.Create("skein", "Skeincoin"),
                Algorithm.Create("skein2", "Woodcoin"),
                Algorithm.Create("skunk", "Signatum"),
                Algorithm.Create("timetravel ", "MachineCoin"),
                Algorithm.Create("tribus", "Denarius"),
                Algorithm.Create("x11evo", "Revolver"),
                Algorithm.Create("x11", "DarkCoin"),
                Algorithm.Create("x14", "X14Coin"),
                Algorithm.Create("x15", "Halcyon"),*/
                Algorithm.Create("x16r", "x16r"),
                Algorithm.Create("x16s", "x16s"),
                Algorithm.Create("phi", "phi"),
                Algorithm.Create("bitcore", "Bitcore"),
                Algorithm.Create("x17", "x17"),
                Algorithm.Create("vit", "Vitality"),
                Algorithm.Create("Xevan", "Xevan"),
                Algorithm.Create("tribus", "Tribus"),
                Algorithm.Create("cll", "Cll"),
                Algorithm.Create("skunk", "Skunk"),
                Algorithm.Create("timetravel", "TimeTravel8"),
                Algorithm.Create("poly", "Polytimos"),
                Algorithm.Create("aeriumx", "AeriumX"),
                Algorithm.Create("sonoa", "Sonoa"),
                Algorithm.Create("phi2", "phi2"),
                /*Algorithm.Create("x17", "X17"),
                Algorithm.Create("x17", "X17"),
                Algorithm.Create("vanilla", "Vanilla(Blake256)"),
                Algorithm.Create("veltor", "VeltorCoin"),
                Algorithm.Create("whirlpool", "Joincoin"),
                Algorithm.Create("wildkeccak", "Boolberry(Stratum only)"),
                Algorithm.Create("zr5", "ZiftrCoin"),*/
            };
            _algorithms.Sort((s1, s2) => s1.Name.CompareTo(s2.Name));
        }

        public string FindAlgorithmName(string algorithmName)
        {
            return Algorithm.FindByArgument(algorithmName, _algorithms).Name;
        }

        public List<string> GetList()
        {
            return Algorithm.GetNames(_algorithms);
        }

        public void Run(IConfig config)
        {
            var algorithm = Algorithm.Find(config.Algorithm, _algorithms).Argument;
            _minerCli.Run(config, algorithm);
        }

        public MinerReport GetMinerReport()
        {
            return _minerCli.MinerReport;
        }

        public void ResetMinerReport()
        {
            _minerCli.MinerReport = new MinerReport();
        }

        private void _minerCli_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            ErrorDataReceived?.Invoke(sender, e);
        }

        private void _minerCli_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            OutputDataReceived?.Invoke(sender, e);
        }

        public void Stop()
        {
            _minerCli.Close();
        }

        public bool IsRunning
        {
            get
            {
                return !_minerCli.Closed;
            }
        }

        private List<Algorithm> _algorithms;
        private MinerCli _minerCli;
    }
}
