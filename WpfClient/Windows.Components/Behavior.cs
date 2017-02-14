
namespace XData.Windows.Components
{
    public abstract class Behavior : System.Windows.FrameworkElement
    {
        protected object AssociatedObject
        {
            get;
            private set;
        } 
 
        public void Attach(object obj)
        {
            if (obj != AssociatedObject)
            {              
                AssociatedObject = obj;
                OnAttached();
            }
        }

        protected abstract void OnAttached();
        
    }

    public abstract class Behavior<T> : Behavior
    {
        protected new T AssociatedObject
        {
            get
            {
                return (T)base.AssociatedObject;
            }
        }
    }


}
