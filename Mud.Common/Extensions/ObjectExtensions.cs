namespace Mud.Common.Extensions
{
    public static class ObjectExtensions
    {
        public static T CastTo<T>(this object instance) where T : class
        {
            return instance as T;
        }
    }
}
