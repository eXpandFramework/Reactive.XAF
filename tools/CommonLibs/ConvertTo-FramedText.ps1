Function cf{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0, valueFromPipeline = $true)]
        [string]$stringIN,
        [string]$char = "-",
        [switch]$NoRoof
    )

    $underLine = $char * $stringIn.length
    if (!$NoRoof){
        $underLine
    }
    
    $stringIn

    $underLine

} 