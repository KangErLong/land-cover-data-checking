# land-cover-data-checking
Inconsisteny Detection for Land Cover Update
Configurations: Microsoft Visual Studio 2010 and Esri ArcEngine 10.2 C# Language
menu File -> data loading （land cover baselin, land cover update and cglc_chkmark.shp）
menu Rule Discovery-Number of Spatial Relation -> quantifying spatial relationships including surround, surrounded-by, disjoint and connnect with
    parameters input include land cover types geocoded
    conf_inter.py calculate the confidence interval, and decide which relationship is forbidden
menu Existing Rules-Spaital Existing Rules -> data inconsistency detection by mautiple matching
cglc_chkmark.shp stores the labelled point which refers to spatiotemproal land cover object incosistency occured in land cover update
