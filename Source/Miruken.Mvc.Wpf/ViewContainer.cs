﻿namespace Miruken.Mvc.Wpf
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using Callback;
    using Views;

    public abstract class ViewContainer : Panel, IViewRegion, IView
    {
        private ViewPolicy _policy;

        [EditorBrowsable(EditorBrowsableState.Never),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ViewPolicy Policy
        {
            get { return _policy ?? (_policy = new ViewPolicy(this)); }
            set { _policy = value; }
        }

        [EditorBrowsable(EditorBrowsableState.Never),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object ViewModel
        {
            get { return DataContext; }
            set { DataContext = value; }
        }

        public virtual IViewLayer Display(IViewRegion region)
        {
            return region?.Show(this);
        }

        public V View<V>() where V : IView
        {
            return View<V>(null, HandleMethod.RequireComposer());
        }

        public V View<V>(Action<V> init) where V : IView
        {
            return View(init, HandleMethod.RequireComposer());
        }

        protected virtual V View<V>(Action<V> init, IHandler composer)
            where V : IView
        {
            if (!Dispatcher.CheckAccess())
            {
                return (V) Dispatcher.Invoke(
                    new Func<Action<V>, Handler, V>(View),
                    init, composer);
            }

            V view;
            if (typeof(V).IsInterface)
            {
                var type = GetType();
                if (!typeof(V).IsAssignableFrom(type))
                    return Handler.Unhandled<V>();
                view = (V)Activator.CreateInstance(type);
            }
            else
                view = Activator.CreateInstance<V>();

            init?.Invoke(view);

            var element = view as FrameworkElement;
            if (element != null)
            {
                if (element.DataContext == null)
                    element.DataContext = composer.Resolve<IController>();
                var controller = element.DataContext as IController;
                controller.DependsOn(view);
            }

            view.Policy.Track();
            return view;
        }

        public IViewLayer Show<V>() where V : IView
        {
            var composer = HandleMethod.RequireComposer();
            return Dispatcher.CheckAccess()
                 ? Show(View<V>(null, composer), composer)
                 : Dispatcher.Invoke(() => Show(View<V>(null, composer), composer));
        }

        public IViewLayer Show<V>(Action<V> init) where V : IView
        {
            var composer = HandleMethod.RequireComposer();
            return Dispatcher.CheckAccess()
                 ? Show(View(init, composer), composer)
                 : Dispatcher.Invoke(() => Show(View(init, composer), composer));
        }

        public IViewLayer Show(IView view)
        {
            return Show(view, HandleMethod.RequireComposer());
        }
        
        protected abstract IViewLayer Show(IView view, IHandler composer);
    }
}
