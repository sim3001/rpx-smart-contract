using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace RPX_Sales
{
    public class RPX_Sales : SmartContract
    {
        //Token Settings
        public static string Name() => "Red Pulse Token";
        public static string Symbol() => "RPX";
        public static readonly byte[] presale_1 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static readonly byte[] presale_2 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static readonly byte[] presale_3 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static readonly byte[] public_token_sales = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static readonly byte[] company_reserve = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static readonly byte[] shareholders = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static readonly byte[] employees = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static readonly byte[] red_pulse_platform = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()

        //Token Sales Settings
        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197};
        private const ulong basic_rate = 1000 * factor;
        private const ulong neo_decimals = 100000000;
        private const ulong total_neo = 405844 * neo_decimals;
        private const ulong pre_1_sales_tokens = 33768 * basic_rate * 150 / 100;
        private const ulong pre_2_sales_tokens = 64977 * basic_rate * 140 / 100;
        private const ulong pre_3_sales_tokens = 25000 * basic_rate * 140 / 100;
        private const ulong pre_total_neo = 123745 * neo_decimals;
        private const ulong once_max_neo = 1352 * neo_decimals ;
        private const ulong one_hour_neo = 27 * neo_decimals;
        private const string invest_times_prefix = "MINT_COUNT";
        private const int sales_start_time = 1507467600;
        private const int sales_end_time = 1508677200;

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("refund")]
        public static event Action<byte[], BigInteger> Refund;

        public static BigInteger TotalSalesNeo() => total_neo;

        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(public_token_sales);
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "deploy") return Deploy();
                if (operation == "mintTokens") return MintTokens();
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "decimals") return Decimals();
                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }
                if (operation == "inflation") return Inflation();
                if (operation == "inflationRate")
                {
                    if (args.Length != 1) return false;
                    BigInteger rate = (BigInteger)args[0];
                    return InflationRate(rate);
                }
                if (operation == "inflationStartTime")
                {
                    if (args.Length != 1) return false;
                    BigInteger start_time = (BigInteger)args[0];
                    return InflationStartTime(start_time);
                }
                if (operation == "queryInflationRate") return QueryInflationRate();
                if (operation == "queryInflationStartTime") return QueryInflationStartTime();
                if (operation == "totalSalesNeo") return TotalSalesNeo();
                if (operation == "salesNeo") return SalesNeo();
                if (operation == "inner") return Inner();
            }
            return false;
        }

        // initialization parameters, only once
        // 初始化参数
        public static bool Deploy()
        {
            if (!Runtime.CheckWitness(public_token_sales)) return false;
            byte[] total_supply = Storage.Get(Storage.CurrentContext, "totalSupply");
            if (total_supply.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, presale_1, pre_1_sales_tokens);
            Storage.Put(Storage.CurrentContext, presale_2, pre_2_sales_tokens);
            Storage.Put(Storage.CurrentContext, presale_3, pre_3_sales_tokens);
            Storage.Put(Storage.CurrentContext, "totalSupply", pre_1_sales_tokens + pre_2_sales_tokens + pre_3_sales_tokens);
            Storage.Put(Storage.CurrentContext, "salesNeo", pre_total_neo);
            Transferred(null, presale_1, pre_1_sales_tokens);
            Transferred(null, presale_2, pre_2_sales_tokens);
            Transferred(null, presale_3, pre_3_sales_tokens);
            return true;
        }

        // The function MintTokens is only usable by the chosen wallet
        // contract to mint a number of tokens proportional to the
        // amount of neo sent to the wallet contract. The function
        // can only be called during the tokenswap period
        // 将众筹的neo转化为等价的ico代币
        public static bool MintTokens()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput reference = tx.GetReferences()[0];
            // check whether asset is neo
            // 检查资产是否为neo
            if (reference.AssetId != neo_asset_id) return false;
            byte[] sender = reference.ScriptHash;
            TransactionOutput[] outputs = tx.GetOutputs();
            byte[] receiver = ExecutionEngine.ExecutingScriptHash;
            ulong value = 0;
            // get the total amount of Neo
            // 获取转入智能合约地址的Neo总量
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == receiver)
                {
                    value += (ulong)output.Value;
                }
            }
            uint now = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            int time = (int)now - sales_start_time;
            // the current exchange rate between sales tokens and neo during the token swap period
            // 获取众筹期间token和neo间的转化率
            ulong swap_rate = CurrentSwapRate(time);
            // crowdfunding failure
            // 众筹失败
            if (swap_rate == 0)
            {
                Refund(sender, value);
                return false;
            }
            value = InvestCapacity(time, sender, value);
            if(value == 0)
            {
                return false;
            }
            // crowdfunding success
            // 众筹成功
            ulong token = value / neo_decimals * swap_rate;
            BigInteger balance = Storage.Get(Storage.CurrentContext, sender).AsBigInteger();
            Storage.Put(Storage.CurrentContext, sender, token + balance);
            BigInteger totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            Storage.Put(Storage.CurrentContext, "totalSupply", token + totalSupply);
            Transferred(null, sender, token);
            return true;
        }

        // get the total token supply
        // 获取已发行token总量
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }

        // function that is always called when someone wants to transfer tokens.
        // 流转token调用
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Transferred(from, to, value);
            return true;
        }

        // get the account balance of another account with address
        // 根据地址获取token的余额
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

        public static bool Inflation()
        {
            BigInteger rate = Storage.Get(Storage.CurrentContext, "inflationRate").AsBigInteger();
            if (rate == 0) return false;
            BigInteger start_time = Storage.Get(Storage.CurrentContext, "inflationStartTime").AsBigInteger();
            if (start_time == 0) return false;
            uint now = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            int time = (int)now - (int)start_time;
            if (time < 0) return false;
            BigInteger total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            int n = time / 86400 + 1;
            BigInteger day_inflation = 0;
            BigInteger n_day_inflation = 0;
            for (int i = 0; i < n; i++)
            {
                day_inflation = total_supply * rate / 1000000000000;
                n_day_inflation += day_inflation;
                total_supply += day_inflation;
            }
            Storage.Put(Storage.CurrentContext, "totalSupply", total_supply);
            Storage.Put(Storage.CurrentContext, "inflationStartTime", start_time + n * 86400);
            BigInteger owner_token = Storage.Get(Storage.CurrentContext, red_pulse_platform).AsBigInteger();
            Storage.Put(Storage.CurrentContext, red_pulse_platform, owner_token + n_day_inflation);
            Transferred(null, red_pulse_platform, owner_token + n_day_inflation);
            return true;
        }
        public static bool InflationRate(BigInteger rate)
        {
            if (!Runtime.CheckWitness(red_pulse_platform)) return false;
            Storage.Put(Storage.CurrentContext, "inflationRate", rate);
            return true;
        }

        public static BigInteger QueryInflationRate()
        {
            return Storage.Get(Storage.CurrentContext, "inflationRate").AsBigInteger();
        }

        public static bool InflationStartTime(BigInteger start_time)
        {
            if (!Runtime.CheckWitness(red_pulse_platform)) return false;
            BigInteger inflation_start_time = Storage.Get(Storage.CurrentContext, "inflationStartTime").AsBigInteger();
            if (inflation_start_time != 0) return false;
            Storage.Put(Storage.CurrentContext, "inflationStartTime", start_time);
            return true;
        }

        public static BigInteger QueryInflationStartTime()
        {
            return Storage.Get(Storage.CurrentContext, "inflationStartTime").AsBigInteger();
        }

        public static BigInteger SalesNeo()
        {
            return Storage.Get(Storage.CurrentContext, "salesNeo").AsBigInteger();
        }

        public static bool Inner()
        {
            if (!Runtime.CheckWitness(company_reserve)) return false;
            BigInteger inner_flag = Storage.Get(Storage.CurrentContext, "inner_flag").AsBigInteger();
            if(inner_flag != 0) return false;
            BigInteger total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            BigInteger total_tokens = total_supply * 100 / 40;
            Storage.Put(Storage.CurrentContext, "inner_flag", "1");
            BigInteger compangy_reserve_tokens = total_supply;
            BigInteger shareholders_tokens = total_tokens * 15 / 100;
            BigInteger employees_tokens = total_tokens * 5 / 100;
            Storage.Put(Storage.CurrentContext, company_reserve, compangy_reserve_tokens);
            Storage.Put(Storage.CurrentContext, shareholders, shareholders_tokens);
            Storage.Put(Storage.CurrentContext, employees, employees_tokens);
            Storage.Put(Storage.CurrentContext, "totalSupply", total_supply + compangy_reserve_tokens + shareholders_tokens + employees_tokens);
            return true;
        }

        // The function CurrentSwapRate() returns the current exchange rate
        // between tokens and neo during the token swap period
        private static ulong CurrentSwapRate(int time)
        {
            const int token_sales_duration = sales_end_time - sales_start_time;
            if (time < 0)
            {
                return 0;
            }
            else if (time < 86400)
            {
                return basic_rate * 130 / 100;
            }
            else if (time < 259200)
            {
                return basic_rate * 120 / 100;
            }
            else if (time < 604800)
            {
                return basic_rate * 110 / 100;
            }
            else if (time < token_sales_duration)
            {
                return basic_rate;
            }
            else
            {
                return 0;
            }
        }

        private static ulong InvestCapacity(int time, byte[] sender, ulong value)
        {
            Byte[] invest_times_key = invest_times_prefix.AsByteArray().Concat(sender);
            BigInteger invest_times = Storage.Get(Storage.CurrentContext, invest_times_key).AsBigInteger();
            // 个人投资上限
            // personal invest capacity
            if (time <= 3600)
            {
                if (invest_times > 0)
                {
                    Refund(sender, value);
                    return 0;
                }
                if (value > one_hour_neo)
                {
                    Refund(sender, value - one_hour_neo);
                    value = one_hour_neo;
                }
                Storage.Put(Storage.CurrentContext, invest_times_key, 1);
            }
            else
            {
                if (invest_times > 1)
                {
                    Refund(sender, value);
                    return 0;

                }
                if (value > once_max_neo)
                {
                    Refund(sender, value - once_max_neo);
                    value = once_max_neo;
                }
                Storage.Put(Storage.CurrentContext, invest_times_key, 2);
            }
            // 总募集上限
            // crowdfunding capacity
            BigInteger sales_neo = Storage.Get(Storage.CurrentContext, "salesNeo").AsBigInteger();
            BigInteger balance_neo = total_neo - sales_neo;
            if (total_neo <= sales_neo)
            {
                Refund(sender, value);
                return 0;
            }
            else if (balance_neo <= value)
            {
                Refund(sender, value - balance_neo);
                sales_neo += balance_neo;
                value = (ulong) balance_neo;
            }
            else
            {
                sales_neo += value;
            }
            Storage.Put(Storage.CurrentContext, "salesNeo", sales_neo);
            return value;
        }
    }
}
