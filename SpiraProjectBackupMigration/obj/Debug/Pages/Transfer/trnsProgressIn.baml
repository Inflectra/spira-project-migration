   M S B A M L   `   `   `  ����  ProjectMigrationC    =Inflectra.SpiraTest.Utilities.ProjectMigration.trnsProgressIn   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      .��5      �   � ColumnDefinitions  5	      �� 5
      $��* �6    � RowDefinitions 5      �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5      �� $
��Auto��6   $
��Wrap=�6   $��2��60   $��0��6D   
�5      �� 5      $
��Bold"�6
   $��16(�6   .�6*    Importing Project To SpiraTeam6J   �� 5      �� 5      .�6
   64Currently importing saved project into new project '6@   �� 6E   -   5         �Name$ txtProjectName��5   I   $�	[Project]��6_   �� 6s   .�6w   '.6{   5      �� 5      -   5        $��	txtAction��5   )   $��1��6   $
��Auto��6   $��[Action]��6:   $��0,25,0,0q�6J   �� 5      -   5        $��pnlBar��5   O   $��2��6   $
��Auto��6   $��5q�6&   $��Stretch=�61   .+�5      � -   5        $��barProgress��5   /   $��Stretch=�6    � IsIndeterminate .  � Maximum$ 1Q�6Y    � Minimum$ 0Q�6e   $��Stretch=�6q   
 �� Top$ 0��6�    �� Right$ 0��6�    �� Bottom$ 0��6�   	 �� Left$	 0��6�   ��6�   �� 
 �� ElementName$
 pnlBar�� �� Path$ ActualWidth���6  �� $
 txtPercentage��$ ActualHeight�6F  �� 5      -   5        $��txtPercentage��5      $
��Auto��6$   $��0%��62    �� TextAlignment$ Center=�6<   ��6S   �� $
 pnlBar��$ ActualWidth�5      �� 5      -   5        $��txtError��5   )   $��3��6   $
��Auto��6   $�� ��69   $��0,25,0,0q�6A   
���  ��$
��Wrap=�6d   $y�	Collapsed=�6x   �� 5      -   5        $ txtErrorMessage��5   	   �� 6#   �� 60   .�64   ��The log file containing error information was saved to the current desktop. The created project may not be in a useable state, and it is recommended that the project be deleted through the application.6�   5       5!      5"      