using System;
using System.Collections.Generic;
using TongbaoExchangeCalc.DataModel;

namespace TongbaoExchangeCalc.Undo.Commands
{
    public class PlayerDataCommand : IUndoCommand
    {
        protected readonly PlayerData mPlayerData;
        private readonly Func<bool> mPlayerFunc;

        private PlayerData mPlayerDataBefore;
        private PlayerData mPlayerDataAfter;

        public PlayerDataCommand(PlayerData playerData, Func<bool> playerFunc)
        {
            mPlayerData = playerData ?? throw new ArgumentNullException(nameof(playerData));
            mPlayerFunc = playerFunc;
        }

        protected PlayerData ClonePlayerData()
        {
            var playerData = new PlayerData(mPlayerData.TongbaoSelector, mPlayerData.Random);
            playerData.CopyFrom(mPlayerData);
            return playerData;
        }

        public virtual bool Execute()
        {
            mPlayerDataBefore = ClonePlayerData();
            bool result = ExecuteInternal();
            mPlayerDataAfter = ClonePlayerData();
            return result;
        }

        protected virtual bool ExecuteInternal()
        {
            return mPlayerFunc?.Invoke() ?? false;
        }

        public virtual void Redo()
        {
            mPlayerData.CopyFrom(mPlayerDataAfter);
        }

        public virtual void Undo()
        {
            mPlayerData.CopyFrom(mPlayerDataBefore);
        }
    }

    public class ExchangeTongbaoCommand : PlayerDataCommand
    {
        public ExchangeTongbaoCommand(PlayerData playerData, int slotIndex)
            : base(playerData, () => playerData.ExchangeTongbao(slotIndex)) { }
    }
}
