// Converters are defined in their own files:
//   BooleanToColorConverter.cs  - converts bool to a Color/Brush (used for database status indicator)
//
// All converters are registered as Application.Resources in App.xaml:
//   <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
//   <converters:BooleanToColorConverter x:Key="BooleanToColorConverter"/>
