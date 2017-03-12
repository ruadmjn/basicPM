<?php

$variable = "nothing criminal";
echo $variable;

//criminal++
$variable = $_SERVER["PHP_SELF"];
$variable = (int)$_SERVER["PHP_SELF"];
$variable = myfunc($_SERVER["PHP_SELF"]);
echo $variable;

//criminal++
echo not_known_func($_SERVER["PHP_SELF"]);
echo ($_GET["login"]);

?>