# Computer Graphics Labs

My projects for the CG1 course @ PW
- WPFFilters was the image filtering algorithms for Labs 1 & 2,  
- WPFDrawing was the shape drawing/filling algorithms for Labs 3 & 4

Everything was done with just WPF and no graphics libraries,  
each operation was done directly on the respective pixel buffer, by hand

(**TIP**: click a gif/heading for a full HD demo :wink:)

## [Image Filtering Algorithms](https://www.youtube.com/watch?v=hxlHTQnwwKA&list=PLk6u9j48w-dbzpphLHDXKCHdaLYC8Dco-)

[![Image Filtering App Demo](./_extras/WPFFiltersDemo480.gif)](https://www.youtube.com/watch?v=hxlHTQnwwKA&list=PLk6u9j48w-dbzpphLHDXKCHdaLYC8Dco-)

Cool stuff that wasn't part of the tasks:
- When editing a Filter at an arbitrary position in the "currently applied" Collection,  
don't naively recompute the effect of the entire collection (that would get *very* slow whenever convolution filters are involved),  
but instead use a hashing trick to only recompute the filters which *need* to be recomputed  
- Pre-computing lookup tables for the pixel-by-pixel filters,  
which greatly speeds up things like gamma filters which have math that doesn't play well with realtime refreshing

Biggest things I learned:
- Filtering algorithms aren't too hard, but propagation of change is quite non-trivial
- Creational design patterns are *very* important :sweat_smile:

## [Drawing / Filling Algorithms](https://www.youtube.com/watch?v=HnmM0ssyFgI&list=PLk6u9j48w-dbzpphLHDXKCHdaLYC8Dco-)

[![Drawing App Demo](./_extras/WPFDrawingDemo480.gif)](https://www.youtube.com/watch?v=HnmM0ssyFgI&list=PLk6u9j48w-dbzpphLHDXKCHdaLYC8Dco-)

Biggest things I learned:
- Propagation of change is absolutely non-trivial
- Message passing can be brutal. :sob:

