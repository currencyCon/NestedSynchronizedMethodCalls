using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestedSynchronizedMethodCalss
{
    class BankAccount
    {
        private int balance;
        public void Deposit(int amount)
        {
            lock (this) { balance += amount; }
        }
        public void Transfer(BankAccount target, int amount)
        {
            lock (this)
            {
                balance -= amount;
                target.Deposit(amount); // lock (target)
            }
        }
    }
}
