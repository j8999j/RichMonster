public enum PlayerInfoPage
{
    None = 0,
    Bag = 1,
    SouvenirBag = 2,
    Achievement = 3,
    Book = 4,
    News = 5,
    Contract = 6,
    SouvenirShop = 7
}

public interface IPlayerInfoPage
{
    void OpenPage();
    void ClosePage();
}
