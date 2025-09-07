# wprowadzone zmiany
## zamiana stringów na string buildery w FivetranConnectionSupport
w przypadku wielokrotnej konkatenacji stringów powstaje spory nadkład związany z ciągłym alokowaniem całego stringa na nowo
## naprawa tworzenia i zwalniania semaforu
wcześniejszy semafor miał początkową wartość ustawioną na zero, przezco na start żaden wątek nie był się w stanie dostać do sekcji krytycznej
dodatkowo semafor był pozyskiwany wielokrotmnie w przypadku ponawiania zapytań przez jeden wątek, po czym był zwalniany tylko jednokrotnie
W tym celu zajęcie semafora dodałem do funkcji pomocniczej
W końcu, w przypadku wystąpienia błędu semafor nie był nidy zwalniany, dlatego dodałem go w bloku finnaly
## TTlDictionary
klasa gdy zapytana równolegle o tą samą wartość mogła by wysłać dwa zapytania, dlatego dodałem semafor w celu zapobiegania temu
dodatkowo, zmieniłem typ zwracany przez GetOrAdd na value task
##  PaginatedFetcher  
znajdował się tam komętarz twierdzący, że wywołanie pobrania kolejnej strony i jej parsowania to jest "fire and forget"
ale osobiście uważam, że jest to bardziej prefetch
