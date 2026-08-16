namespace Wick.Core;

public interface IPlayerWick
{
    int CurrentRadius { get; }
    bool Spend(int amount);
}

public interface ITurnAdvancer
{
    void AdvanceTurn();
}
