using System.Windows;

namespace XData.Windows.Components
{
    public static class BehaviorService
    {
        public static void SetBehavior(DependencyObject obj, Behavior value)
        {
            value.Attach(obj);
        }
    }
}
