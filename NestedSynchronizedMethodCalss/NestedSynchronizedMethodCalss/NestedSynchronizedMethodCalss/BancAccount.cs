using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestedSynchronizedMethodCalss
{
    class BankAccount : IBankAccount
    {
        private int _balance;

        public int Balance
        {
            get
            {
                return _balance;
            }
            set { _balance = value; }
        }

        public void Deposit(int amount)
        {
            lock (this) { Balance += amount; }
        }
        public void Transfer(IBankAccount target, int amount)
        {
            lock (this)
            {
                Balance -= amount;
                target.Deposit(amount); // lock (target)
            }
        }
    }

    internal interface IBankAccount
    {
        void Transfer(IBankAccount target, int amount);
        void Deposit(int amount);
    }
    
}
