   M S B A M L   `   `   `  ����K  GProjectMigration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=nullo<clr-namespace:Inflectra.SpiraTest.Utilities.ProjectMigration.Inflectra.SpiraTest.Utilities.ProjectMigration  5      E    ?Inflectra.SpiraTest.Utilities.ProjectMigration.trnsSpiraNewUser   5      W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      Kmy<clr-namespace:Inflectra.SpiraTest.Utilities.ProjectMigration     5      #��� ��5	      .��5      �   � ColumnDefinitions  5   
   �� 5      $	��126 �6   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   �� $
��Auto��6   $
��Wrap=�6"   $��2��66   $��0��6J   
�5      �� 5      $
��Bold"�6   $��16(�6(   .�66   Imported User Password:6O   �� 5      �� 5      .�6   ��If any new users have to be imported, they need to have a default password, and security question and answer. The new password entered below must meet security requirements in the application under Administration -> Security.6�   �� 5      �� 5      .�6   :8Use the Test button below to test your default password.6P   5      �� 5   
   $��1��6   $��0��6!   $��Default Password:��61   $��5,25,5,5q�6K   ]� 5    
   $��1��6   $��1��6!   $��Center=�61   $��Stretch=�6L   ��. $��5,25,5,5q�6   .+�5!      �� -   5        $��Right=�5!      $��5,0,5,5q�6,   $
��Test��6=    � ToolTip$C =Test the password on security settings with the Spira server.��6L   $��5,0,5,0q�6�   �� 5"      -   5        $��txtDefaultPassword��5"   3   $��2��6   $��1��6#    �� 	MaxLength$ 50��6M   $��0,0,5,5q�6\   5#      �� 5$   
   -   5        $��
divTesting��5$   E   $��1��6   $��1��6!   $y�	Collapsed=�6.   $��5,25,5,5q�6W   #��� ��6i   $ Click to hide.��6�   ._�5%      ]� ��. $��Stretch=�6-   $	��Top=�6K   .+�5&      E   ?Inflectra.SpiraTest.Utilities.ProjectMigration.ucntSpinnerApple  -   5          Name$ 
spnSpinner��5&   &   $��Center=�6:   $��20��6U   $��20��6a   $
��Left=�6l   �� 5'      -   5        $��
txtTesting��5'   p    �� TextAlignment$ Center=�6   $��Testing.��63   $
��Bold"�6C   $��Center=�6U   $
��Wrap=�6�   
���   �$��Stretch=�6�   $��0,0,20,0q�6  ��6�   ��  �� ElementName$ cmbApplicationServer�� �� Path$ ActualWidth�6  5(      5)      �� 5*   
   $��2��6   $��0��6!   $��Security Question:��61   $��5q�6L   �� 5+   
   -   5        $��txtQuestion��5+      $��2��6'   $��1��64   $��5q�6D   $
��Bold"�6O   �� 5,   
   $��3��6   $��0��6!   $��Security Answer:��61   $��5q�6J   �� 5-   
   -   5        $��	txtAnswer��5-      $��3��6%   $��1��62   $��5q�6B   $
��Bold"�6M   5.      5/      