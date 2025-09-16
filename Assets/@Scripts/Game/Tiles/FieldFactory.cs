namespace Game.Tiles
{
    public abstract class FieldFactory
    {
        public abstract Field Create(int sizeX, int sizeY, int sizeZ);
    }
}
