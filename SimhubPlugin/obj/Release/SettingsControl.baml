   M S B A M L   `   `   `  ���� J  FAPR.DashSupport, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null1clr-namespace:APR.DashSupportAPR.DashSupport  P LSimHub.Plugins, Version=1.0.9034.18950, Culture=neutral, PublicKeyToken=nullU;clr-namespace:SimHub.Plugins.Styles;assembly=SimHub.PluginsSimHub.Plugins.Styles M7clr-namespace:SimHub.Plugins.UI;assembly=SimHub.PluginsSimHub.Plugins.UI %    APR.DashSupport.SettingsControl   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     @d2http://schemas.microsoft.com/expression/blend/2008     /localclr-namespace:APR.DashSupport     Nstyles;clr-namespace:SimHub.Plugins.Styles;assembly=SimHub.Plugins     Fui7clr-namespace:SimHub.Plugins.UI;assembly=SimHub.Plugins     .��(  "SimHub.Plugins.Styles.SHTabControl  -      Name$  
tabcontrol��.@�%  SimHub.Plugins.Styles.SHTabItem  $��Dash settings��.���� .���� .+�%  SimHub.Plugins.Styles.SHSection     Title$ Settings��.���� .+��  � ColumnDefinitions �� $	��250 ��� $	��100 ��� $	��250 ��� $��1* � � RowDefinitions �� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30��.+��� $��0��$"��Show Brake and throttle bars��$
��Left=�$��Center=� J� RenderTransformOrigin$ 0.5,0.5&�$��18��*  $SimHub.Plugins.Styles.SHToggleButton  $��0��$��1��$
��Left=�$	��Top=�$��20��$ 0.45,0.2&� d� 	IsChecked �� *EnableBrakeAndThrottleBars+�� $��0��$��2��$��Show RPM Bar��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��0��$��3��$
��Left=�$	��Top=�$��20��$ 0.45,0.2&� �� *EnableRPMBar+�� $��1��$��0��$��Target Brake Pecentage��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� $��1��$��1��$
��Left=�$	��Top=�$��20��$��50��$ 0.45,0.2&����� *BrakeTargetPercentage+ �� ValidatesOnDataErrors .  �� ValidatesOnExceptions .  �� StringFormat$ {0:#0.0}���� $��1��$��2��$��Peak Brake Pecentage��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� $��1��$��3��$
��Left=�$	��Top=�$��20��$��50��$ 0.45,0.2&����� *BrakeMaxPercentage+ .  . $ {0:#0.0}���� $��2��$��0��$!��Trail Brake start Pecentage��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� $��2��$��1��$
��Left=�$	��Top=�$��20��$��50��$ 0.45,0.2&����� *BrakeTrailStartPercentage+ .  . $ {0:#0.0}���� $��2��$��2��$ ��Trail Brake end Percentage��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� $��2��$��3��$
��Left=�$	��Top=�$��20��$��50��$ 0.45,0.2&����� *BrakeTrailEndPercentage+ .  . $ {0:#0.0}���� $��4��$��0��$%��Preferred brake bias Percentage��$
��Left=�$	��Top=�$ 0.5,0.5&�$��18��$��1,8,0,0q��� $��4��$��1��$
��Left=�$��Center=�$��20��$��50��$ 0.45,0.2&�$��0,0,0,0q����� *PreferredBrakeBiasPercentage+ .  . $ {0:#0.0'%'}���� $��6��$��0��$��Show Pit window Pop-up��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��6��$��1��$
��Left=�$	��Top=�$��20��$ 0.45,0.2&� �� *EnablePitWindowPopup+�� $��6��$��2��$%��Percentage around track to show��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� $��6��$��3��$
��Left=�$	��Top=�$��20��$��50��$ 0.45,0.2&����� *PitWindowPopupPercentage+ .  . $ {0:#0.0'%'}���� $��7��$��0��$"��Show Fuel consumption Pop-up��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��7��$��1��$
��Left=�$	��Top=�$��20��$ 0.45,0.2&� �� *EnableFuelPopup+�� $��7��$��2��$%��Percentage around track to show��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� $��7��$��3��$
��Left=�$	��Top=�$��20��$��50��$ 0.45,0.2&����� *FuelPopupPercentage+ .  . $ {0:#0.0'%'}���� $��8��$��0��$!��Prefer full throttle starts��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��8��$��1��$
��Left=�$	��Top=�$��20��$ 0.45,0.2&� �� *PreferFullThrottleStarts+�� $��8��$��2��$-��'Adjust Bite point for track temperature��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��8��$��3��$
��Left=�$	��Top=�$��20��$ 0.45,0.2&� �� *&$AdjustBiteRecommendationForTrackTemp+�� $��9��$��0��$)��#Using dualclutch paddles for starts��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��9��$��1��$
��Left=�$	��Top=�$��20��$ 0.45,0.2&� �� *LaunchUsingDualClutchPaddles+  $��Pitstop Strategy��.���� .���� .+�  $" Pitstop Strategy Calculation��.���� .+��  �� $	��500 � �� $��1* �$��50��.+��� $��0��$��0��$����These settings are used for pit strategy calculations. They are intended for multi-stop races and provide multiple strategy calculations for the driver to choose from.��$	��Top=�$ 0.5,0.5&�$��WrapWithOverflow=�$��
0,0,-206,0q��  �� $	��250 ��� $	��100 ��� $	��250 ��� $��1* � �� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30��.+��� $)��#Enable Pitstop Strategy Calculation��$��Center=�$ 0.5,0.5&�$��18��$��3��$��	0,0,200,0q�  $��0��$��1��$
��Left=�$	��Top=�$��20��$ 0.45,0.2&� �� *EnableStrategyCalculation+�� $��1��$
��Bold"�$@��:Values to override calculation defaults. Use with caution.��$��Center=�$ 0.5,0.5&�$��18��$��3��$��	0,0,200,0q��� $��2��$��0��$0��*Starting Fuel Percentage (Default is 100%)��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� $��2��$��1��$
��Left=�$	��Top=�$��20��$��50��$ 0.45,0.2&����� *(&StrategyOverrideStartingFuelPercentage+ .  . $ {0:#0.0}���� $��3��$��0��$+��%Available tank size (Default is 100%)��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� $��3��$��1��$
��Left=�$	��Top=�$��20��$��50��$ 0.45,0.2&����� *(&StrategyOverrideStartingFuelPercentage+ .  . $ {0:#0.0}��  $��	Standings��$��32��$	��Top=�.���� .���� .+�  $ 	Standings��.���  �� $	��200 ��� $	��100 ��� $	��200 ��� $��1* � �� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30���� $��1* �$��30��.+��� $��0��$��0��$��Enable Standings��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��0��$��1��$
��Left=�$��Center=�$��20��$ 0.45,0.2&� �� *EnableStandings+�� $��0��$��2��$��Driver Name Style��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� -   $��0��$��3��$
��Left=�$��Center=�$	��200��$��
0,10,-15,0q�.@��� 	 �� 
IsSelected	 �� *DriverNameStyle_0+.��	Full Name�� 	 �� *DriverNameStyle_1+.��Firstname Lastname�� 	 �� *DriverNameStyle_2+.��Lastname, Firstname�� 	 �� *DriverNameStyle_3+.��F. Lastname�� 	 �� *DriverNameStyle_4+.��Firstname L.�� 	 �� *DriverNameStyle_5+.��Lastname, F.�� 	 �� *DriverNameStyle_6+.��LAS�� $��2��$��0��$��Show Position��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��2��$��1��$
��Left=�$��Center=�$��20��$ 0.45,0.2&� �� *ColumnShowPosition+�� $��2��$��2��$��Show car number��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��2��$��4��$
��Left=�$��Center=�$��20��$ 0.45,0.2&� �� *ColumnShowCarNumber+�� $��3��$��0��$��Show Gap to Leader��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��3��$��1��$
��Left=�$��Center=�$��20��$ 0.45,0.2&� �� *ColumnShowGapToLeader+�� $��3��$��2��$��Show Gap car in front��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��3��$��4��$
��Left=�$��Center=�$��20��$ 0.45,0.2&� �� *ColumnShowGapToCarInFront+�� $��4��$��0��$��Show last lap��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��4��$��1��$
��Left=�$��Center=�$��20��$ 0.45,0.2&� �� *ColumnShowLastLap+�� $��4��$��2��$��Show fastest lap��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��4��$��4��$
��Left=�$��Center=�$��20��$ 0.45,0.2&� �� *ColumnShowFastestLap+�� $��5��$��0��$��Cycle gaps and lap times��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��5��$��1��$
��Left=�$��Center=�$��20��$ 0.45,0.2&� �� *ColumnCycleDisplay+�� $��5��$��2��$!��Cycle data every N seconds.��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� $��5��$��3��$
��Left=�$	��Top=�$��20��$��50��$ 0.45,0.2&����� *ColumnCycleDuration+ .  . $ {0:#0.0}���� $��6��$��0��$��Slide out fastest lap��$
��Left=�$��Center=�$ 0.5,0.5&�$��18��  $��6��$��1��$
��Left=�$��Center=�$��20��$ 0.45,0.2&� �� *ColumnSlideOutFastestLap+�� $��6��$��2��$%��Show fastest lap for N seconds.��$
��Left=�$��Center=�$ 0.5,0.5&�$��18���� $��6��$��3��$
��Left=�$	��Top=�$��20��$��50��$ 0.45,0.2&����� *ColumnSlideOutDuration+ .  . $ {0:#0.0}��