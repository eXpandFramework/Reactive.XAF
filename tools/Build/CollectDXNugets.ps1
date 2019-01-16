param([string]$path="$PSScriptRoot\..\..",$modulesPath="*\src\*")

Get-ChildItem $path *.csproj -Recurse|Where-Object{$_.FullName -like $modulesPath}|ForEach-Object {
    [xml]$csproj = Get-Content $_.FullName
    $dxVersion=$csproj.Project.PropertyGroup.DXVersion|Where-Object{$_ -ne $null}
    if (!$dxVersion){
        $dxVersion="0.0.0.0"
    }
    
    $csproj.Project.ItemGroup.Reference.Include|Where-Object{$_ -like "DevExpress*"}|ForEach-Object {
        
        $assemblyName =[System.Text.RegularExpressions.Regex]::Replace($_,".v[\d]{2}\.\d","")
        $commaIndex=$assemblyName.IndexOf(",")
        
        if ($commaIndex -gt -1){
            $assemblyName=$assemblyName.Substring(0,$commaIndex)
        }
        
        if ($assemblyName -like "DevExpress.Xtra*") {
            if ($assemblyName -like "*XtraRichEdit*") {
                [PSCustomObject]@{
                    Name = "DevExpress.Win.RichEdit"
                    Version=$dxVersion
                }
            }
            elseif ($assemblyName -like "*XtraChart*") {
                if ($assemblyName -like "*Wizard*" -or $assemblyName -like "*Extenions*" -or $assemblyName -like "*UI*") {
                    [PSCustomObject]@{
                        Name = "DevExpress.Win.Charts"
                        Version=$dxVersion
                    }
                }
                else{
                    [PSCustomObject]@{
                        Name = $assemblyName.Replace("Xtra", "")
                        Version=$dxVersion
                    }
                }
            }
            elseif ($assemblyName -like "*XtraGauges*") {
                
                if ($assemblyName -like "*Win*") {
                    [PSCustomObject]@{
                        Name = "DevExpress.Win.Gauges"
                        Version=$dxVersion
                    }
                }
                else {
                    [PSCustomObject]@{
                        Name = $assemblyName.Replace("Xtra", "")
                        Version =$dxVersion
                    }
                }
            }
            else {
                [PSCustomObject]@{
                    Name = "DevExpress.Win"
                    Version=$dxVersion
                }
            }
        }
        elseif ($assemblyName -like "DevExpress.Web.ASPx*") {
            [PSCustomObject]@{
                Name = "DevExpress.Web"
                Version=$dxVersion
            }
        }
        elseif ($assemblyName -like "DevExpress.Web.Resources*") {
            [PSCustomObject]@{
                Name = "DevExpress.Web"
                Version=$dxVersion
            }
        }
        elseif ($assemblyName -like "DevExpress.Workflow.Activities*") {
            [PSCustomObject]@{
                Name = "DevExpress.ExpressApp.Workflow"
                Version=$dxVersion
            }
        }
        elseif ($assemblyName -like "DevExpress.BonusSkins*") {
            [PSCustomObject]@{
                Name = "DevExpress.Win.BonusSkins"
                Version=$dxVersion
            }
        }
        elseif ($assemblyName -like "DevExpress.Docs*") {
            [PSCustomObject]@{
                Name = "DevExpress.Document.Processor"
                Version=$dxVersion
            }
        }
        elseif ($assemblyName -like "DevExpress.Dashboard.Web*") {
            [PSCustomObject]@{
                Name = "DevExpress.Web.Dashboard"
                Version=$dxVersion
            }
        }
        elseif ($assemblyName -like "DevExpress.Dashboard.Win*") {
            [PSCustomObject]@{
                Name = "DevExpress.Win.Dashboard"
                Version=$dxVersion
            }
        }    
        elseif ($assemblyName -like "DevExpress.Dashboard*") {
            if ($assemblyName -like "*WebForms*") {
                [PSCustomObject]@{
                    Name = "DevExpress.Web.Dashboard"
                    Version=$dxVersion
                }
            }
            elseif ($assemblyName -like "*Win*") {
                [PSCustomObject]@{
                    Name = "DevExpress.Win.Dashboard"
                    Version=$dxVersion
                }
            }
        }    
        elseif ($assemblyName -eq "DevExpress.XtraCharts.Web") {
            [PSCustomObject]@{
                Name = "DevExpress.Web.Visualization"
                Version=$dxVersion
            }
        }
        elseif ($assemblyName -like "*XtraReport*") {
            if ($assemblyName -like "*Extensions*") {
                [PSCustomObject]@{
                    Name = "DevExpress.Win.Reporting"
                    Version=$dxVersion
                }
            }
            elseif ($assemblyName -like "*WebForms*" ) {
                [PSCustomObject]@{
                    Name = "DevExpress.Web.Reporting"
                    Version=$dxVersion
                }
            }
            elseif ($assemblyName -like "*.Web.*" ) {
                [PSCustomObject]@{
                    Name = "DevExpress.Web.Reporting.Common"
                    Version=$dxVersion
                }
            }
            else {
                [PSCustomObject]@{
                    Name = "DevExpress.Reporting.Core"
                    Version=$dxVersion
                }
            }
        }
        elseif ($assemblyName -like "*XtraScheduler*") {
            if ($assemblyName -like "*reporting*") {
                [PSCustomObject]@{
                    Name = "DevExpress.Win.SchedulerReporting"
                    Version=$dxVersion
                }
            }
            elseif ($assemblyName -like "*Core*") {
                [PSCustomObject]@{
                    Name = "DevExpress.Scheduler.Core"
                    Version=$dxVersion
                }
            }
            else {
                [PSCustomObject]@{
                    Name = "DevExpress.Win.Scheduler"
                    Version=$dxVersion
                }
            }
        }
        elseif ($assemblyName -like "*ASPxScheduler*") {
            [PSCustomObject]@{
                Name = "DevExpress.Web.Scheduler"
                Version=$dxVersion
            }
        }
        elseif ($assemblyName -like "*ASPxThemes*") {
            [PSCustomObject]@{
                Name = "DevExpress.Web.Themes"
                Version=$dxVersion
            }
        }
        else{
            [PSCustomObject]@{
                Name = $assemblyName
                Version=$dxVersion
            }
        }
    }
}| Group-Object 'Name','Version' | ForEach-Object{ $_.Group | Select-Object 'Name','Version' -First 1} | Sort-Object 'Name','Version'