import * as React from "react";
import { Eye, EyeOff, AlertTriangle, Check, X } from "lucide-react";
import { cn } from "../../lib/utils";
import { Alert, AlertDescription, AlertTitle, AlertCircleIcon } from "./alert";

export type PasswordRules = {
    minLength?: number;
    maxLength?: number;
    minUppercase?: number;
    minLowercase?: number;
    minDigits?: number;
    minNonAlphanumeric?: number;
    minUniqueChars?: number;
    maxRepeatRun?: number;
};

type PasswordInputProps = Omit<React.ComponentProps<"input">, "type"> & {
    passwordRules?: PasswordRules;
    accountNameForRules?: string;
    emailForRules?: string;
    description?: string,
    descriptionAlign?: "left" | "center" | "right",
    errorMessage?: string
    errorDescription?: React.ReactNode
};

type RuleResult = { key: string; ok: boolean; text: string };

function normalizeForCompare(str: string) {
    return str.toLowerCase().replace(/[^a-z0-9]/g, "");
}
function isDerived(password: string, candidate: string): boolean {
    if (!password || !candidate) return false;
    const normPwd = normalizeForCompare(password);
    const normCand = normalizeForCompare(candidate);
    return normPwd.includes(normCand);
}

function evaluateRules(
    password: string,
    rules: PasswordRules,
    accountNameForRules?: string,
    emailForRules?: string
): RuleResult[] {
    const items: RuleResult[] = [];
    if (!password) return items;

    let upper = 0, lower = 0, digits = 0, nonAlnum = 0, unique = 0, maxRun = 1;
    const seen = new Set<string>();
    let last: string | null = null, run = 1;

    for (const ch of password) {
        if (/[A-Z]/.test(ch)) upper++;
        else if (/[a-z]/.test(ch)) lower++;
        else if (/[0-9]/.test(ch)) digits++;
        else nonAlnum++;
        if (!seen.has(ch)) { seen.add(ch); unique++; }
        if (last !== null && last === ch) { run++; if (run > maxRun) maxRun = run; }
        else { run = 1; last = ch; }
    }

    if (rules.minLength != null)
        items.push({ key: "minLength", ok: password.length >= rules.minLength, text: `Legalább ${rules.minLength} karakter` });
    if (rules.maxLength != null)
        items.push({ key: "maxLength", ok: password.length <= rules.maxLength, text: `Legfeljebb ${rules.maxLength} karakter` });

    const noWhitespace = !/\s/.test(password);
    items.push({ key: "noWhitespace", ok: noWhitespace, text: "Nincs whitespace" });

    if (rules.minUppercase != null)
        items.push({ key: "minUppercase", ok: upper >= rules.minUppercase, text: `Legalább ${rules.minUppercase} nagybetű` });
    if (rules.minLowercase != null)
        items.push({ key: "minLowercase", ok: lower >= rules.minLowercase, text: `Legalább ${rules.minLowercase} kisbetű` });
    if (rules.minDigits != null)
        items.push({ key: "minDigits", ok: digits >= rules.minDigits, text: `Legalább ${rules.minDigits} számjegy` });
    if (rules.minNonAlphanumeric != null)
        items.push({ key: "minNonAlphanumeric", ok: nonAlnum >= rules.minNonAlphanumeric, text: `Legalább ${rules.minNonAlphanumeric} speciális karakter` });
    if (rules.minUniqueChars != null)
        items.push({ key: "minUniqueChars", ok: unique >= rules.minUniqueChars, text: `Legalább ${rules.minUniqueChars} különböző karakter` });
    if (rules.maxRepeatRun != null)
        items.push({ key: "maxRepeatRun", ok: maxRun <= rules.maxRepeatRun, text: `Nincs ${rules.maxRepeatRun}-nál hosszabb ismétlés` });

    if (accountNameForRules) {
        const derived = isDerived(password, normalizeForCompare(accountNameForRules));
        items.push({ key: "notDerivedFromAccount", ok: !derived, text: "Nem származhat a felhasználónévből" });
    }
    if (emailForRules) {
        const local = emailForRules.split("@")[0];
        const derived = isDerived(password, normalizeForCompare(local));
        items.push({ key: "notDerivedFromEmail", ok: !derived, text: "Nem származhat az e-mail címből" });
    }

    return items;
}

export function PasswordInput({
    className,
    passwordRules,
    accountNameForRules,
    emailForRules,
    onChange,
    value: valueProp,
    description,
    descriptionAlign = "left",
    errorMessage,
    errorDescription,
    ...props
}: PasswordInputProps) {
    const [visible, setVisible] = React.useState(false);
    const [capsOn, setCapsOn] = React.useState(false);
    const [canReveal, setCanReveal] = React.useState(true);
    const inputRef = React.useRef<HTMLInputElement | null>(null);
    const clickedRevealRef = React.useRef(false);

    // mindig kontrollált
    const value = (valueProp as string) ?? "";

    React.useEffect(() => {
        console.log("can reveal changed");
        console.log(canReveal);
    }, [canReveal]);

    const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
        if (clickedRevealRef.current) {
            clickedRevealRef.current = false;
            return;
        }
        if (value.trim() !== "") {
            setCanReveal(false);
            setVisible(false);
        }
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if ((e.target.value ?? "") === "") {
            setCanReveal(true);
            setVisible(false);
        }
        onChange?.(e);
    };

    const toggleVisible = () => {
        if (!canReveal) return;
        setVisible(s => !s);
        inputRef.current?.focus();
    };

    const handleKeyUp = (e: React.KeyboardEvent<HTMLInputElement>) => {
        setCapsOn(e.getModifierState && e.getModifierState("CapsLock"));
    };

    const handleRevealMouseDown = (e: React.MouseEvent<HTMLButtonElement>) => {
        clickedRevealRef.current = true;
        e.preventDefault();
        e.stopPropagation();
    };
    const handleRevealPointerDown = (e: React.PointerEvent<HTMLButtonElement>) => {
        clickedRevealRef.current = true;
        e.preventDefault();
        e.stopPropagation();
    };

    const ruleResults =
        passwordRules && value !== ""
            ? evaluateRules(value, passwordRules, accountNameForRules, emailForRules)
            : [];

    const showError = Boolean(errorMessage);

    return (
        <div className="flex flex-col gap-1">
            <div className="relative">
                <input
                    ref={inputRef}
                    type={visible ? "text" : "password"}
                    data-slot="input"
                    autoComplete="new-password"
                    className={cn(
                        "file:text-foreground placeholder:text-muted-foreground selection:bg-primary selection:text-primary-foreground dark:bg-input/30 border-input flex h-9 w-full min-w-0 rounded-md border bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm",
                        "focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]",
                        "aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive",
                        "pr-10",
                        showError && "border-destructive focus-visible:border-destructive focus-visible:ring-destructive/30",
                        className
                    )}
                    value={value}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    onKeyUp={handleKeyUp}
                    {...props}
                />

                <button
                    type="button"
                    aria-label={visible ? "Jelszó elrejtése" : "Jelszó megjelenítése"}
                    onMouseDown={handleRevealMouseDown}
                    onPointerDown={handleRevealPointerDown}
                    onClick={toggleVisible}
                    className={cn(
                        "absolute right-2 top-1/2 -translate-y-1/2 inline-flex items-center justify-center rounded-sm",
                        "text-muted-foreground hover:text-foreground transition",
                        !canReveal && "opacity-50 cursor-not-allowed"
                    )}
                    tabIndex={-1}
                >
                    {visible ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
            </div>

            {capsOn && (
                <div className="flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400">
                    <AlertTriangle className="h-3.5 w-3.5" />
                    <span>Be van kapcsolva a Caps Lock.</span>
                </div>
            )}

            {errorMessage && (
                <Alert variant="destructive" noBorder>
                    <AlertCircleIcon />
                    <AlertTitle>{errorMessage}</AlertTitle>
                    {errorDescription && (
                        <AlertDescription>{errorDescription}</AlertDescription>
                    )}
                </Alert>
            )}

            {passwordRules && value !== "" && ruleResults.length > 0 && (
                <ul className="mt-1 space-y-1">
                    {ruleResults.map(r => (
                        <li
                            key={r.key}
                            className={cn(
                                "text-xs flex items-start gap-1",
                                r.ok ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"
                            )}
                        >
                            {r.ok ? (
                                <Check className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                            ) : (
                                <X className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                            )}
                            <span>{r.text}</span>
                        </li>
                    ))}
                </ul>
            )}

            {description && (
                <p
                    className={cn(
                        "text-xs text-muted-foreground",
                        descriptionAlign === "center" && "text-center",
                        descriptionAlign === "right" && "text-right"
                    )}
                >
                    {description}
                </p>
            )}
        </div>
    );
}
