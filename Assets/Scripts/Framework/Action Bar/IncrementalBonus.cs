namespace Game
{
    public class  IncrementalBonus : ActionBarItem
    {
        public override float CostMultiplier => 1.0f;

        public override bool IsClickable()
        {
            return CurrencyManager.Instance.GetCurrencyAmount(costCurrency) >= GetCost();
        }

        public override bool IsVisible()
        {
            return true;
        }

        public override void OnClick()
        {
            CurrencyManager.Instance.AddCurrency(costCurrency.currencyID, -GetCost());
            CurrentLevel++;
        }
    }
}