import { NumericFormat } from 'react-number-format'
import { Controller, type Control, type FieldPath, type FieldValues } from 'react-hook-form'
import { cn } from '@/shared/lib/cn'

interface CurrencyInputProps<TFieldValues extends FieldValues> {
  name: FieldPath<TFieldValues>
  control: Control<TFieldValues>
  className?: string
}

/** BRL-formatted currency input bound to react-hook-form — value is always a plain number (reais, not cents). */
export function CurrencyInput<TFieldValues extends FieldValues>({
  name,
  control,
  className,
}: CurrencyInputProps<TFieldValues>) {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field: { onChange, value, ref, onBlur }, fieldState: { error } }) => (
        <NumericFormat
          getInputRef={ref}
          value={value ?? ''}
          onBlur={onBlur}
          thousandSeparator="."
          decimalSeparator=","
          prefix="R$ "
          decimalScale={2}
          fixedDecimalScale
          allowNegative={false}
          onValueChange={(values) => onChange(values.floatValue ?? 0)}
          className={cn(
            'input text-right font-mono tabular-nums',
            error && 'border-danger',
            className,
          )}
        />
      )}
    />
  )
}
