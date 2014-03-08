#pragma once

#ifdef NDEBUG
$WCMIXED?#error Has:// No$ mixed revisions
$WCMODS?#error Has:// No$ local modifications
$WCUNVER?#error Has:// No$ unversioned items
#endif

#define FILEVERSIONDOT "1.1.0.$WCREV$"
#define FILEVERSIONCOMMA 1,1,0,$WCREV$
#define COPYRIGHT "© Randon Spackman 2013-$WCDATE=%Y$"
