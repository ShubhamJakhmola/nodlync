using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AIHub.Models;
using AIHub.ViewModels;

namespace AIHub.Views
{
    public partial class ProjectsPage : UserControl
    {
        public ProjectsPage()
        {
            InitializeComponent();
        }

        private void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ProjectsViewModel vm)
            {
                return;
            }

            ProjectsListView.SelectedItem = null;
            vm.BeginCreateProject();
        }

        private void ProjectsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not ProjectsViewModel vm)
            {
                return;
            }

            vm.SelectProject(ProjectsListView.SelectedItem as Project);
        }

        private void ProjectListItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && FindAncestor<Button>(source) != null)
            {
                return;
            }

            if (DataContext is not ProjectsViewModel vm || sender is not ListViewItem item || item.DataContext is not Project project)
            {
                return;
            }

            item.IsSelected = true;
            vm.SelectProject(project);
        }

        private void ProjectListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && FindAncestor<Button>(source) != null)
            {
                return;
            }

            if (DataContext is not ProjectsViewModel vm || sender is not ListViewItem item || item.DataContext is not Project project)
            {
                return;
            }

            item.IsSelected = true;
            vm.SelectProject(project);

            if (vm.OpenWorkspaceCommand.CanExecute(project))
            {
                vm.OpenWorkspaceCommand.Execute(project);
            }
        }

        private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
        {
            var current = source;
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
