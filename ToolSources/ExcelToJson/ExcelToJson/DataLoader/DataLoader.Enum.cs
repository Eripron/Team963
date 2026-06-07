namespace Tool
{
    public partial class DataLoader
    {
        public int GetEnumValue(string enumName, string strValue)
        {
            return Program.EnumRepository.GetValueIndex(enumName, strValue);
        }
    }
}
