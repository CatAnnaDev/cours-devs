CC ?= cc
CFLAGS ?= -std=c17 -O2 -Wall -Wextra

clings: clings.c catalogue.c catalogue.h
	$(CC) $(CFLAGS) -o clings clings.c catalogue.c

run: clings
	./clings

verify: clings
	./clings verify

clean:
	rm -f clings
	rm -rf .travail

.PHONY: run verify clean

compile_commands.json:
	@printf '[\n' > $@
	@first=1; for f in exercices/*/*.c solutions/*/*.c; do \
		if [ $$first -eq 0 ]; then printf ',\n' >> $@; fi; first=0; \
		printf '  {"directory": "%s", "file": "%s", "command": "cc -std=c17 -Wall -Wextra -I. -c %s"}' \
			"$$(pwd)" "$$f" "$$f" >> $@; \
	done
	@printf '\n]\n' >> $@
	@echo "compile_commands.json genere"
